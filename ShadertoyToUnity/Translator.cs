using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ShadertoyToUnity
{
    internal enum ShaderType
    {
        VERTEX_SHADER = 0,
        FRAGMENT_SHADER = 1
    }

    internal struct ShaderUniform
    {
        public string name;
        public string data_type; // float4x4, float4
    }

    internal struct ShaderVarying
    {
        public string name;
        public string data_type; // float4x4, float4
    }

    internal struct ShaderAttribute
    {
        public string name;
        public string data_type; // float4x4, float4
    }

    internal struct ShaderFunction
    {
        public string func_name;
        public string func_return_type;
        public string func_params;
        public string func_definition;
    }

    internal class Translator
    {
        private readonly List<ShaderAttribute> m_attribs = new List<ShaderAttribute>();
        private string m_filename;
        private readonly List<ShaderFunction> m_funcs = new List<ShaderFunction>();
        private readonly Dictionary<string, string> m_map_replacements = new Dictionary<string, string>();

        private ShaderType m_shader_type;
        private readonly List<ShaderUniform> m_uniforms = new List<ShaderUniform>();

        private readonly List<ShaderVarying> m_varyings = new List<ShaderVarying>();

        public Translator()
        {
            InitDictionary();
        }

        private void InitDictionary()
        {
            m_map_replacements["vec2"] = "float2";
            m_map_replacements["vec3"] = "float3";
            m_map_replacements["vec4"] = "float4";
            m_map_replacements["mat4"] = "float4x4";
            m_map_replacements["mat3"] = "float3x3";
            m_map_replacements["lowp"] = "";
            m_map_replacements["mediump"] = "";
            m_map_replacements["highp"] = "";
            m_map_replacements["mix"] = "lerp";
            m_map_replacements["gl_Position"] = "v_position";
            m_map_replacements["gl_PointSize"] = "float psize";

            m_map_replacements["texture2D"] = "tex2D";
        }

        public static string RemoveComments(string a_text)
        {
            // remove comments
            var blockComments = @"/\*(.*?)\*/";
            var versionComments = @"#(.*?)\r?\n";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            var results_no_comments = Regex.Replace(a_text,
                blockComments + "|" + lineComments + "|" + versionComments + "|" + strings + "|" + verbatimStrings,
                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("//") || me.Value.StartsWith("#"))
                        return me.Value.StartsWith("//") ? Environment.NewLine : "";
                    // Keep the literal strings
                    return me.Value;
                },
                RegexOptions.Singleline);

            return results_no_comments;
        }

        private string MassageContents(string a_contents)
        {
            var result = a_contents;

            // replace matching pairs
            foreach (var entry in m_map_replacements)
                result = result.Replace(entry.Key, entry.Value);

            var results_no_comments = RemoveComments(result);

            // remove newlines and tabs
            results_no_comments = results_no_comments.Replace("\r\n", " ");
            results_no_comments = results_no_comments.Replace("\t", "  ");

            // collapses multiple spaces into one
            var regex = new Regex(@"[ ]{2,}", RegexOptions.None);
            results_no_comments = regex.Replace(results_no_comments, @" ");

            return results_no_comments;
        }

        private string GetToken(ref string a_contents)
        {
            var content = a_contents;
            content = a_contents.TrimStart();

            if (content == "") return "";

            var last_index =
                content.IndexOfAny(new[]
                    {' ', '*', '<', '>', '-', '+', '/', '%', '(', ')', '{', '}', ',', ';', '[', ']'});

            // if zero character, remove one
            last_index = Math.Max(last_index, 1);

            // extract token
            var token = content.Substring(0, last_index);

            // move the cursor forward
            a_contents = content.Remove(0, last_index);

            return token;
        }

        private string GetFuncParams(ref string a_contents)
        {
            a_contents = a_contents.TrimStart();

            var last_index = a_contents.IndexOf(')');

            var params_inside = a_contents.Substring(0, last_index);

            // consume contents
            a_contents = a_contents.Remove(0, last_index + 1);
            a_contents = a_contents.TrimStart();

            return params_inside;
        }

        private string GetFuncDef(ref string a_contents)
        {
            var content = a_contents;
            var last_index = 0;
            var open_bracket_count = 1;

            while (open_bracket_count > 0)
            {
                var curr_index_open = content.IndexOf('{', last_index);
                var curr_index_closed = content.IndexOf('}', last_index);

                if ((curr_index_closed < curr_index_open) || (curr_index_open <= 0))
                {
                    last_index = content.IndexOf('}', last_index);
                    --open_bracket_count;

                    if (open_bracket_count > 0)
                        ++last_index;
                }

                else
                {
                    last_index = curr_index_open + 1;
                    ++open_bracket_count;
                }
            }

            var func_def = content.Substring(0, last_index);
            func_def = func_def.Trim();

            a_contents = content.Remove(0, last_index + 1);

            return func_def;
        }

        private void Parse(string a_contents)
        {
            var str_top = a_contents;

            while (str_top.Length > 0)
            {
                // get the token
                var token = GetToken(ref str_top);

                // attribute
                if (token == "attribute")
                {
                    var data_type = GetToken(ref str_top);
                    var name = GetToken(ref str_top);
                    var semi_colon = GetToken(ref str_top);

                    Debug.Assert(semi_colon == ";");

                    ShaderAttribute entry;
                    entry.data_type = data_type;
                    entry.name = name;
                    m_attribs.Add(entry);
                }

                // uniforms
                else if (token == "uniform")
                {
                    var data_type = GetToken(ref str_top);
                    var name = GetToken(ref str_top);
                    var semi_colon = GetToken(ref str_top);

                    Debug.Assert(semi_colon == ";");

                    ShaderUniform entry;
                    entry.data_type = data_type;
                    entry.name = name;
                    m_uniforms.Add(entry);
                }

                // varying
                else if (token == "varying")
                {
                    var data_type = GetToken(ref str_top);
                    var name = GetToken(ref str_top);
                    var semi_colon = GetToken(ref str_top);

                    Debug.Assert(semi_colon == ";");

                    ShaderVarying entry;
                    entry.data_type = data_type;
                    entry.name = name;
                    m_varyings.Add(entry);
                }

                else if (token == "")
                {
                    break;
                }

                // function
                else
                {
                    var func_return_type = token;
                    var func_name = GetToken(ref str_top);

                    // open paren - start of func params
                    var char_open_paren = GetToken(ref str_top);
                    Debug.Assert(char_open_paren == "(");

                    var func_params = GetFuncParams(ref str_top);

                    // open bracket - start of function def
                    var char_open_bracket = GetToken(ref str_top);
                    Debug.Assert(char_open_bracket == "{");

                    var func_definition = GetFuncDef(ref str_top);

                    // make entry
                    ShaderFunction entry;
                    entry.func_name = func_name;
                    entry.func_return_type = func_return_type;
                    entry.func_params = func_params;
                    entry.func_definition = func_definition;

                    m_funcs.Add(entry);
                }
            }
        }

        private ShaderFunction GetFunction(string a_func_name)
        {
            Debug.Assert(m_funcs.Count > 0);

            foreach (var func in m_funcs)
                if (func.func_name == a_func_name)
                    return func;

            return m_funcs[0];
        }

        private string GetAttribsParamsStr()
        {
            var output = "";

            foreach (var param in m_attribs)
            {
                output += string.Format("\t{0} {1},", param.data_type, param.name);
                output += Environment.NewLine;
            }

            return output;
        }

        private string GetUniformsParamsStr()
        {
            var sampler_count = 0;

            var output = "";

            foreach (var param in m_uniforms)
                if (param.data_type == "sampler2D")
                {
                    output += string.Format("\t{0} {1} {2} : TEXUNIT{3},", "uniform", param.data_type, param.name,
                        sampler_count);
                    output += Environment.NewLine;

                    ++sampler_count;
                }
                else
                {
                    output += string.Format("\t{0} {1} {2},", "uniform", param.data_type, param.name);
                    output += Environment.NewLine;
                }

            return output;
        }

        private string GetVaryingsParamsStr()
        {
            var output = "";

            var varying_type = m_shader_type == ShaderType.VERTEX_SHADER ? "out" : "in";

            if (m_shader_type == ShaderType.VERTEX_SHADER)
            {
                output += string.Format("\t{0} {1} {2} : {3},", "float4", varying_type, "v_position", "POSITION");
                output += Environment.NewLine;
            }

            var texcoord_count = 0;
            foreach (var param in m_varyings)
            {
                var tex_coord_str = "TEXCOORD" + texcoord_count;
                ++texcoord_count;

                output += string.Format("\t{0} {1} {2} : {3},", param.data_type, varying_type, param.name, tex_coord_str);
                output += Environment.NewLine;
            }

            return output;
        }

        private string GetMainFuncParams()
        {
            // main func params
            var main_func_params = "";

            main_func_params += GetAttribsParamsStr();
            main_func_params += GetUniformsParamsStr();
            main_func_params += GetVaryingsParamsStr();

            // remove last comma
            var last_comma_index = main_func_params.LastIndexOf(',');
            if (last_comma_index > 0)
                main_func_params = main_func_params.Substring(0, last_comma_index);

            main_func_params += Environment.NewLine;
            return main_func_params;
        }

        private string GetNonMainFunctionStr()
        {
            var output = "";

            foreach (var func in m_funcs)
            {
                if (func.func_name == "main")
                    continue;

                output += string.Format("{0} {1} ({2})", func.func_return_type, func.func_name, func.func_params) +
                          Environment.NewLine;
                output += "{" + Environment.NewLine;
                output += GetFunctionDefinitionFormated(func.func_definition) + Environment.NewLine;
                output += "}" + Environment.NewLine;
                output += Environment.NewLine;
            }

            return output;
        }

        private string GetFunctionDefinitionFormated(string a_func_definition)
        {
            // replace output of fragment shader
            var regex_spacing = new Regex(@"gl_FragColor[ ]{0,}=[ ]{0,}", RegexOptions.None);
            var results = regex_spacing.Replace(a_func_definition, @"return ");

            // replace output of fragment shader
            var regex_semicolons = new Regex(@";[ ]{0,}", RegexOptions.None);
            results = regex_semicolons.Replace(results, @";" + Environment.NewLine + '\t');

            //
            results = Regex.Replace(results, @"(u_mvp)[ ]{0,}[*][ ]{0,}(.*?);",
                m =>
                    string.Format(
                        "mul( {1}, {0} );",
                        m.Groups[1].Value,
                        m.Groups[2].Value));


            return "\t" + results;
        }

        private string OutputCgVertexShader()
        {
            var main_func = GetFunction("main");

            var output = "";

            output += "// Auto-generated from GLSL vertex shader" + Environment.NewLine;
            output += GetNonMainFunctionStr();
            output += "void main(" + Environment.NewLine;
            output += GetMainFuncParams();
            output += "\t)" + Environment.NewLine;
            output += "{" + Environment.NewLine;

            output += GetFunctionDefinitionFormated(main_func.func_definition) + Environment.NewLine;
            output += "}" + Environment.NewLine;

            return output;
        }

        private string OutputCgFragmentShader()
        {
            var main_func = GetFunction("main");

            var output = "";

            output += "// Auto-generated from GLSL fragment shader" + Environment.NewLine;
            output += GetNonMainFunctionStr();
            output += "float4 main(" + Environment.NewLine;
            output += GetMainFuncParams();
            output += "\t)" + Environment.NewLine;
            output += "{" + Environment.NewLine;
            output += GetFunctionDefinitionFormated(main_func.func_definition) + Environment.NewLine;
            output += "}" + Environment.NewLine;

            return output;
        }

        private string BuildCgShader()
        {
            if (m_shader_type == ShaderType.VERTEX_SHADER)
                return OutputCgVertexShader();
            if (m_shader_type == ShaderType.FRAGMENT_SHADER)
                return OutputCgFragmentShader();

            Debug.Fail("Undefined shader type");
            return "ERROR";
        }

        public void LoadFile(string a_file_in, string a_file_out, ShaderType a_shader_type)
        {
            m_shader_type = a_shader_type;

            Console.WriteLine("Loading file: {0}", a_file_in);
            m_filename = a_file_in;

            var streamReader = new StreamReader(a_file_in, Encoding.UTF8);
            var file_contents = streamReader.ReadToEnd();
            streamReader.Close();

            // convert strings and remove comments
            var contents_massaged = MassageContents(file_contents);

            /*Console.WriteLine("---- OLD FILE ---");
            Console.WriteLine(file_contents);
            Console.WriteLine("---- MASSAGED FILE ---");
            Console.WriteLine(contents_massaged);*/

            Parse(contents_massaged);

            var cg_output = BuildCgShader();

            //Console.WriteLine("---- OUTPUT FILE ---");
            //Console.WriteLine(cg_output);

            File.WriteAllText(a_file_out, cg_output);

            var shader_type = m_shader_type == ShaderType.VERTEX_SHADER ? "vertex" : "fragment";
            Console.WriteLine("Translated {0} shader to file: {1}", shader_type, a_file_out);
        }
    }
}