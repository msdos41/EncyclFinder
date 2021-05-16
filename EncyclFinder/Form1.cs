using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Web;
using System.Net;
using Microsoft.Win32;                          //注册表
using System.Text.RegularExpressions;           //正则表达式

namespace EncyclFinder
{
    public partial class Form1 : Form
    {
        public static List<FileInfo> _lstFileAll = new List<FileInfo>();
        public static List<string> _lstFilePathAll = new List<string>();
        public static SortedDictionary<string, string> _lstDicSearchResult = new SortedDictionary<string, string>();
        public static int _iCountSearch = 0;
        public static string _sRootPath = "";
        
        public Form1()
        {
            InitializeComponent();

            _sRootPath = GetCATIAPath();
            if (_sRootPath=="")
            {
                MessageBox.Show("Cannot find CATIA program in computer!");
                Application.Exit();
            }
        }

        public string GetCATIAPath()
        {
            try
            {
                string softName = "CNEXT";
                string strKeyName = string.Empty;
                string softPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
                RegistryKey regKey = Registry.LocalMachine;
                RegistryKey regSubKey = regKey.OpenSubKey(softPath + softName + ".exe", false);

                object objResult = regSubKey.GetValue(strKeyName);
                RegistryValueKind regValueKind = regSubKey.GetValueKind(strKeyName);
                if (regValueKind == Microsoft.Win32.RegistryValueKind.String)
                {
                    return objResult.ToString();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return "";
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            _iCountSearch++;
            this.listBoxResult.Items.Clear();
            this.webBrowserContent.Navigate("about:blank");
            _lstDicSearchResult.Clear();

            if (_iCountSearch==1)
            {
                int iIndex = _sRootPath.IndexOf("win_b64");
                if (iIndex<0)
                {
                    MessageBox.Show("CATIA Program path NOT include win_b64 folder!");
                    Application.Exit();
                }
                string sPath = _sRootPath.Substring(0, iIndex) + "CAADoc\\Doc\\online\\";
                FindFile(sPath, ref _lstFilePathAll);
                //FindFile("C:\\CATIA\\B26\\CAADoc\\Doc\\online\\",ref _lstFilePathAll);
            }

            string sInput = textBoxInput.Text;
            
            SearchFilesFromKeyWord(sInput, _lstFilePathAll, ref _lstDicSearchResult);
            //
            //int iSize = _lstSearchResult.Count;
            //for (int i = 0; i < iSize;i++ )
            //{
            //    string sTitle = _lstSearchResultTitle[i];
            //    if (sTitle=="")
            //    {
            //        string sPath = _lstSearchResultPath[i];
            //        int iIndex = sPath.LastIndexOf("\\");
            //        sTitle = sPath.Substring(iIndex + 1, sPath.Length - iIndex - 1);
            //    }
            //    listBoxResult.Items.Add(sTitle);
            //}
            foreach (string sKey in _lstDicSearchResult.Keys)
            {
                string sTitle = sKey;
                listBoxResult.Items.Add(sTitle);
            }
        }

        private void listBoxResult_MouseDoubleClick(object sender, EventArgs e)
        {
            //int iSelectIndex = listBoxResult.SelectedIndex;
            //if (iSelectIndex>=0)
            //{
            //    string sPath = _lstSearchResultPath[iSelectIndex];
            //    this.webBrowserContent.Navigate(sPath);
            //}
            string sTitle = (string)listBoxResult.SelectedItem;
            string sPath = _lstDicSearchResult[sTitle];
            this.webBrowserContent.Navigate(sPath);
        }

        public void FindFile(string dirPath,ref List<string> oLstPath)
        {
            DirectoryInfo Dir=new DirectoryInfo(dirPath);
            try
            {
                foreach (DirectoryInfo d in Dir.GetDirectories())    //查找子目录
                {

                    //listBoxResult.Items.Add(d.ToString() + "\\"); //listBox1中填加目录名
                    FindFile(Dir + d.ToString() + "\\",ref oLstPath);

                }

                foreach (FileInfo f in Dir.GetFiles("*.*"))    //查找文件
                {
                    if (f.Extension.Equals(".htm") || f.Extension.Equals(".html"))
                    {
                        //listBoxResult.Items.Add(f.ToString()); //listBox1中填加文件名
                        oLstPath.Add(Dir.ToString()+f.ToString());
                        
                    }
                }
            }

            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void GetContentFromHTML(string sPath, ref string oContent)
        {
            try
            {
                //指定请求
                WebRequest request = WebRequest.Create(sPath);

                //得到返回
                WebResponse response = request.GetResponse();

                //得到流
                Stream recStream = response.GetResponseStream();

                //编码方式
                Encoding ec = Encoding.UTF8;

                //指定转换为gb2312编码
                StreamReader sr = new StreamReader(recStream, ec);

                //以字符串方式得到网页内容
                oContent = sr.ReadToEnd();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void SearchFilesFromKeyWord(string isKeyWord, List<string> ilstFileAll, ref SortedDictionary<string,string> olstResults)
        {
            foreach (string sPath in ilstFileAll)
            {
                string sContent="";
                GetContentFromHTML(sPath, ref sContent);
                //int iIndex = sContent.IndexOf(isKeyWord);
                if (KeyWordsIsMatch(sContent,isKeyWord))
                {
                    sContent = sContent.Replace("<Title>", "<title>");
                    sContent = sContent.Replace("<TITLE>", "<title>");
                    sContent = sContent.Replace("</Title>", "</title>");
                    sContent = sContent.Replace("</TITLE>", "</title>");
                    
                    //获取文档中的title
                    string sTitle = "";
                    int iIndexTitleStart = sContent.IndexOf("<title>");
                    int iIndexTitleEnd = sContent.IndexOf("</title>");
                    if (iIndexTitleStart<iIndexTitleEnd && iIndexTitleStart>-1)
                    {
                        sTitle = sContent.Substring(0, iIndexTitleEnd);
                        iIndexTitleStart = iIndexTitleStart+7;
                        sTitle = sTitle.Substring(iIndexTitleStart, sTitle.Length - iIndexTitleStart);
                    }
                    sTitle = sTitle.Replace("\n", " ");
                    while (0==sTitle.IndexOf(" "))
                    {
                        sTitle = sTitle.Substring(1, sTitle.Length - 1);
                    }

                    //olstResults.Add(sTitle, sPath);
                    olstResults[sTitle] = sPath;
                }
            }
        }

        private bool KeyWordsIsMatch(string isContent, string isKeyWords)
        {
            return Regex.IsMatch(isContent, isKeyWords, RegexOptions.IgnoreCase);
        }
    }

}
