using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace ShadertoyToUnity
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
           DragBtn.PreviewDragOver+=new DragEventHandler(DragBtn_DragOver);
            DragBtn.PreviewDragEnter+=new DragEventHandler(DragBtn_DragOver);
            DragBtn.DragOver += new DragEventHandler(DragBtn_DragOver);
            DragBtn.DragEnter += new DragEventHandler(DragBtn_DragOver);
        }

        private void DragBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btn_drop_files_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
              
            }
        }


        private void DragBtn_DragOver(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            Console.WriteLine(String.Format("Dropped in {0} files", files.Count().ToString()));

            foreach (string file in files)
            {
                Console.WriteLine(String.Format("Processing file: {0}", file));

                string file_in = file;
                string file_ext_in = Path.GetExtension(file);
                string file_ext_out = "";

                ShaderType shader_type = ShaderType.FRAGMENT_SHADER;

                if (file_ext_in == ".vp")
                {
                    shader_type = ShaderType.VERTEX_SHADER;
                    file_ext_out = ".cgvp";
                }
                else if (file_ext_in == ".fp")
                {
                    shader_type = ShaderType.FRAGMENT_SHADER;
                    file_ext_out = ".cgfp";
                }
                else
                {
                    Debug.Fail(String.Format("Unrecognized file type: {0}", file_ext_in));
                }

                string file_out = file_in.Replace(file_ext_in, file_ext_out);

                Translator translator = new Translator();
                translator.LoadFile(file_in, file_out, shader_type);

                /*string file_out = "tex.cgfp";
                ShaderType type = ShaderType.FRAGMENT_SHADER;

                Translator translator = new Translator();
                translator.LoadFile(file_in, file_out, type);*/

            }
        }

        private void DragBtn_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            Console.WriteLine(String.Format("Dropped in {0} files", files.Count().ToString()));

            foreach (string file in files)
            {
                Console.WriteLine(String.Format("Processing file: {0}", file));

                string file_in = file;
                string file_ext_in = Path.GetExtension(file);
                string file_ext_out = "";

                ShaderType shader_type = ShaderType.FRAGMENT_SHADER;

                if (file_ext_in == ".vp")
                {
                    shader_type = ShaderType.VERTEX_SHADER;
                    file_ext_out = ".cgvp";
                }
                else if (file_ext_in == ".fp")
                {
                    shader_type = ShaderType.FRAGMENT_SHADER;
                    file_ext_out = ".cgfp";
                }
                else
                {
                    Debug.Fail(String.Format("Unrecognized file type: {0}", file_ext_in));
                }

                string file_out = file_in.Replace(file_ext_in, file_ext_out);

                Translator translator = new Translator();
                translator.LoadFile(file_in, file_out, shader_type);

                /*string file_out = "tex.cgfp";
                ShaderType type = ShaderType.FRAGMENT_SHADER;

                Translator translator = new Translator();
                translator.LoadFile(file_in, file_out, type);*/

            }
        }

        private void DragBtn_DragEnter(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            Console.WriteLine(String.Format("Dropped in {0} files", files.Count().ToString()));

            foreach (string file in files)
            {
                Console.WriteLine(String.Format("Processing file: {0}", file));

                string file_in = file;
                string file_ext_in = Path.GetExtension(file);
                string file_ext_out = "";

                ShaderType shader_type = ShaderType.FRAGMENT_SHADER;

                if (file_ext_in == ".vp")
                {
                    shader_type = ShaderType.VERTEX_SHADER;
                    file_ext_out = ".cgvp";
                }
                else if (file_ext_in == ".fp")
                {
                    shader_type = ShaderType.FRAGMENT_SHADER;
                    file_ext_out = ".cgfp";
                }
                else
                {
                    Debug.Fail(String.Format("Unrecognized file type: {0}", file_ext_in));
                }

                string file_out = file_in.Replace(file_ext_in, file_ext_out);

                Translator translator = new Translator();
                translator.LoadFile(file_in, file_out, shader_type);

                /*string file_out = "tex.cgfp";
                ShaderType type = ShaderType.FRAGMENT_SHADER;

                Translator translator = new Translator();
                translator.LoadFile(file_in, file_out, type);*/

            }
        }
    }
}
