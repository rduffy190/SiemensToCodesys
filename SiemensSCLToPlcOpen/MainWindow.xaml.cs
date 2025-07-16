using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Collections;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using PlcOpenBuilder;
using System.CodeDom;

namespace SiemensSCLToPlcOpen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = "C:\\";
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();
            // Get the path of specified file
            string filePath = openFileDialog.FileName;
            //Console.WriteLine("Selected file: " + filePath);
            FilePathTextBox.Text = filePath;
            string content = File.ReadAllText(filePath);
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            BuildState _state = BuildState.GetType;
            POUType _type = POUType.Program;
            string _name = "";
            List<Variable> _input = new List<Variable>();
            List<Variable> _output = new List<Variable>();
            List<Variable> _inOut = new List<Variable>();
            List<Variable> _var = new List<Variable>();
            List<Variable> _varConstant = new List<Variable>();
            List<Variable> _varRetain = new List<Variable>();
            List<Variable> _varTemp  = new List<Variable>();
            PlcOpen builder = new PlcOpen("AWL", "Ctrlx", "V1", "GenCode");
            string _st = ""; 

            foreach (string line in lines) {
                switch (_state)
                {
                    case BuildState.GetType:
                        if (line.Contains("FUNCTION_BLOCK"))
                        {
                            _type = POUType.Function_Block;
                            int firstQuote = line.IndexOf('\"');
                            int secondQuote = line.IndexOf('\"', firstQuote + 1);
                            _name = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                            _state = BuildState.GetInterface;
                        }
                        else if (line.Contains("FUNCTION"))
                        {
                            _type = POUType.Function;
                            int firstQuote = line.IndexOf('\"');
                            int secondQuote = line.IndexOf('\"', firstQuote + 1);
                            _name = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                            _state = BuildState.GetInterface;
                        }
                        break;
                    case BuildState.GetInterface:
                        if (line.Contains("VAR_INPUT"))
                            _state = BuildState.GetIn;
                        else if (line.Contains("VAR_OUTPUT"))
                            _state = BuildState.GetOut;
                        else if (line.Contains("VAR_IN_OUT "))
                            _state = BuildState.GetInOut;
                        else if (line.Contains("VAR RETAIN"))
                            _state = BuildState.GetVarRetain;
                        else if (line.Contains("VAR_TEMP"))
                            _state = BuildState.GetVarTemp;
                        else if (line.Contains("VAR CONSTANT"))
                            _state = BuildState.GetVarConstant;
                        else if (line.Contains("VAR"))
                            _state = BuildState.GetVar;
                        else if (line.Contains("BEGIN"))
                            _state = BuildState.GetCode; 
                        break;
                    case BuildState.GetIn:
                        if (line.Contains("END_VAR"))
                            _state = BuildState.GetInterface;
                        else
                            _input.Add(getVariable(line));
                        break;
                    case BuildState.GetOut:
                        if (line.Contains("END_VAR"))
                            _state = BuildState.GetInterface;
                        else
                            _output.Add(getVariable(line));
                        break;
                    case BuildState.GetInOut:
                        if (line.Contains("END_VAR"))
                            _state = BuildState.GetInterface;
                        else
                            _inOut.Add(getVariable(line));
                        break; 
                    case BuildState.GetVar:
                        if (line.Contains("END_VAR"))
                            _state = BuildState.GetInterface;
                        else
                            _var.Add(getVariable(line));
                        break;
                    case BuildState.GetVarRetain:
                        if (line.Contains("END_VAR"))
                            _state = BuildState.GetInterface;
                        else
                            _varRetain.Add(getVariable(line));
                        break;
                    case BuildState.GetVarConstant:
                        if (line.Contains("END_VAR"))
                            _state = BuildState.GetInterface;
                        else
                            _varConstant.Add(getVariable(line));
                        break;
                    case BuildState.GetVarTemp:
                        if (line.Contains("END_VAR"))
                            _state = BuildState.GetInterface;
                        else
                            _varTemp.Add(getVariable(line));
                        break;
                    case BuildState.GetCode:
                        if (!line.Contains("END_FUNCTION"))
                        {
                            string st_line = Regex.Replace(line, @"(?<![tT0-9])#", ""); //Remove any # that isnt t#, T#, or type dec with numeric like 16# and 2#
                            _st = string.Concat(_st,string.Concat(st_line, "\r\n")); //Add the line to the current ST and a new line
                        }
                        else
                            _state = BuildState.Create;
                        break;
                    case BuildState.Create:
                        //Creates all the POU and adds interface and ST Code
                        builder.AddPou(_name, _type);
                        foreach(Variable input in _input)
                        {
                            builder.AddInput(_name, input.Name, input.Type); 
                            if (input.hasStartup)
                            {
                                builder.InitialValue(_name, input.Name, input.Value); 
                            }
                            if (input.hasComment)
                            {
                                builder.VarComment(_name, input.Name, input.comment);
                            }
                        }
                        foreach (Variable output in _output)
                        {
                            builder.AddOutput(_name, output.Name, output.Type);
                            if (output.hasStartup)
                            {
                                builder.InitialValue(_name, output.Name, output.Value);
                            }
                            if (output.hasComment)
                            {
                                builder.VarComment(_name, output.Name, output.comment);
                            }
                        }
                        foreach (Variable inOut in _inOut)
                        {
                            builder.AddInOut(_name, inOut.Name, inOut.Type);
                            if (inOut.hasStartup)
                            {
                                builder.InitialValue(_name, inOut.Name, inOut.Value);
                            }
                            if (inOut.hasComment)
                            {
                                builder.VarComment(_name, inOut.Name, inOut.comment);
                            }
                        }
                        foreach (Variable var in _var)
                        {
                            builder.AddVar(_name, var.Name, var.Type);
                            if (var.hasStartup)
                            {
                                builder.InitialValue(_name, var.Name, var.Value);
                            }
                            if (var.hasComment)
                            {
                                builder.VarComment(_name, var.Name, var.comment);
                            }
                        }
                        foreach (Variable var in _varRetain)
                        {
                            builder.AddPersistentVar(_name, var.Name, var.Type);
                            if (var.hasStartup)
                            {
                                builder.InitialValue(_name, var.Name, var.Value);
                            }
                            if (var.hasComment)
                            {
                                builder.VarComment(_name, var.Name, var.comment); 
                            }
                        }
                        foreach (Variable var in _varConstant)
                        {
                            builder.AddConstVar(_name, var.Name, var.Type);
                            if (var.hasStartup)
                            {
                                builder.InitialValue(_name, var.Name, var.Value);
                            }
                            if (var.hasComment)
                            {
                                builder.VarComment(_name, var.Name, var.comment);
                            }
                        }
                        foreach (Variable var in _varTemp)
                        {
                            builder.AddTemp(_name, var.Name, var.Type);
                            if (var.hasStartup)
                            {
                                builder.InitialValue(_name, var.Name, var.Value);
                            }
                            if (var.hasComment)
                            {
                                builder.VarComment(_name, var.Name, var.comment);
                            }
                        }
                        builder.CreateST(_name, _st);
                        //builder.SaveDoc(_name + ".XML");  Now one doc does all FB/FC in exported file
                        //Clear for next block build
                        _st = "";
                        _input.Clear(); 
                        _output.Clear() ;
                        _var.Clear() ;
                        _inOut.Clear() ;
                        _varTemp.Clear() ;
                        _varConstant.Clear() ;
                        _varRetain.Clear() ;
                        _type = POUType.Program;
                        _name = "";
                        _state = BuildState.GetType;
                       
                        break; 
                }
                builder.SaveDoc("SiemensExport.xml");

            }
        }
        private Variable getVariable(string line)
        {
            Variable variable = new Variable();
            string var_line = Regex.Replace(line, @"\{.*?\}", "");//Replace the Siemens Specific tags
            var_line = var_line.Replace(";", "");//XML doesnt want the ;
            variable.Name = Regex.Replace(var_line.Split(':')[0], @"\s+", "");//Get rid of the white space around the name
            variable.Type = Regex.Replace(var_line.Split(new[] { ":", ":=" }, StringSplitOptions.None)[1].Replace("\"",""), @"\{.*?\}", "");//get the type declaration after : before :=
            if (var_line.Contains(":="))//If there is an init, get it
            {
                variable.hasStartup = true;
                variable.Value = Regex.Replace(var_line.Split(new[] { ":=", "//", "(*" }, StringSplitOptions.None)[1], @"\{.*?\}", "");;//Grab the value that isnt comments or siemens specific tags
            }
            string[] comment = var_line.Split(new[] { "//", "(*" }, StringSplitOptions.None); // get comments for the variable
            if (comment.Length >1)
            {
                string comm = comment[1].Trim().Replace("*)", "");
                variable.comment = comm;
                variable.hasComment = true; 
            }
            return variable;
        }
    }
}