﻿using System;
using System.Collections.Generic;
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
using System.Xml;
using System.IO;
using System.Net;
using System.Windows.Markup;

namespace Загрузка_музыки_из_VK
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class audioItems
        {
            public string title { get; set; }
            public string artist { get; set; }
            public string duration { get; set; }
            public string source { get; set; }
            public bool audioChecked { get; set; }
            public DataTemplate buttonPlay { get; set; }
        public audioItems()
        {
            string xaml = "<DataTemplate><Button Content=\"кнопко\" /></DataTemplate>";
            var sr = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            var pc = new ParserContext();
            pc.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            pc.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            buttonPlay = (DataTemplate)XamlReader.Load(sr, pc);
        }
        }
        static private int appId = 4201412;
        Auth auth;
        VKAPI vkApiClass;
        public string accessToken = "";
        public int userId = 0;
        public bool isAuthorised = false;
        bool searchUserAudio = true; //Искать по записям пользователя. Если false, ищем глобально по сети
        XmlDocument profileXml;
        int currentOffset = 0;
        const int N = 20;
        List<audioItems> currentAudioList; //Список записей, которые сейчас отображены
        List<audioItems> audioToDownload = new List<audioItems>();//Список аудиозаписей, которые нужно скачать
        WebClient client;

        public MainWindow()
        {
            InitializeComponent();
            client = new WebClient();
           
            client.DownloadFileCompleted +=client_DownloadFileCompleted;
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            if (File.Exists("userID.txt"))
            {
                StreamReader streamReader = new StreamReader("userID.txt");
                string str1 = "";
                string str2 = "";

                str1 += streamReader.ReadLine();
                str2 += streamReader.ReadLine();

                //Если уже авторизовывались ранее, читаем access token и user Id из файла.
                if (str1.Length != 0 && str2.Length != 0)
                {
                    accessToken = str1;
                    userId = Convert.ToInt32(str2);
                    vkApiClass = new VKAPI(appId, accessToken);
                    isAuthorised = true;
                    XmlDocument profileXml;
                    string userName = "";
                    profileXml = vkApiClass.getProfile(userId);
                    userName = GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/first_name")) + " " + GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/last_name"));

                    if (!(userName == "нет данных нет данных"))
                    {
                        vkApiClass = new VKAPI(appId, accessToken);
                        button_auth.Background = Brushes.LightGreen;
                        button_auth.Content = "Authorised";

                        profileXml = vkApiClass.getProfile(userId);
                        label_welcome.Content = "User: " + GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/first_name")) +
                            " " + GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/last_name"));
                        getAudioFunc();
                        try
                        {
                            // pictureBox1.Load(GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/photo_50")));
                        }
                        catch (System.Net.WebException)
                        {

                        }
                    }
                    else
                    {
                        MessageBox.Show("Для работы необходимо подключение к сети", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Information);
                        isAuthorised = false;
                    }
                }
                streamReader.Close();
            }
        }    

        private void button_auth_Click(object sender, RoutedEventArgs e)
        {
            if (!isAuthorised)
            {
                auth = new Auth(appId,true);
            }
            else
            {
                auth = new Auth(appId, false);
            }
                auth.Owner = this;
                auth.Closed += auth_Closed;
                auth.Show();                     
        }

        void auth_Closed(object sender, EventArgs e)
        {
            
            if (isAuthorised)
            {
                vkApiClass = new VKAPI(appId, accessToken);
                button_auth.Background = Brushes.LightGreen;
                button_auth.Content = "Logout";
                
                profileXml = vkApiClass.getProfile(userId);
                label_welcome.Content = "User: " + GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/first_name")) +
                    " " + GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/last_name"));
                getAudioFunc(N);
            }
            else
            {
                label_welcome.Content = "User: ";
                if(currentAudioList!=null)
                currentAudioList.Clear();
                button_auth.Background = Brushes.Tomato;
                button_auth.Content = "Login";
            }
        }

        /// <summary>
        /// Получить и вывести в таблицу аудиозаписи пользователя
        /// </summary>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        private void getAudioFunc(int count = 20, int offset = 0)
        {
            XmlNodeList nodeList;
            //item.Clear();
            currentAudioList = new List<audioItems>();
            if (isAuthorised)
            {
                XmlDocument audioXml;
                audioXml = vkApiClass.getAudio(userId, count, offset);
                nodeList = audioXml.SelectNodes("response/audio");
                for(int i=0; i<nodeList.Count; i++){
                    audioItems ite = new audioItems();
                    ite.title = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("title"));
                    ite.artist = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("artist"));
                    ite.duration = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("duration"));
                    ite.source = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("url"));
                    
                    currentAudioList.Add(ite);
                }
                dataGridView1.ItemsSource = currentAudioList;

            }
            else MessageBox.Show("Сначала нужно авторизоваться", "Ошибка", MessageBoxButton.OK,MessageBoxImage.Exclamation);
        }

        //Функция позволяет избежать ошибок, если запрашиваемое поле пусто
        public string GetDataFromXmlNode(XmlNode input)
        {
            if (input == null || String.IsNullOrEmpty(input.InnerText))
            {
                return "нет данных";
            }
            else
            {
                return input.InnerText;
            }
        }

        private void button_find_Click(object sender, RoutedEventArgs e)
        {
            if (textBox_searchGlobalAudio.Text != "")
            {
                getGlobalAudioFunc(N, 0, textBox_searchGlobalAudio.Text);
            }
        }

        /// <summary>
        /// Получить и вывести в таблицу аудиозаписи из всей социальной сети
        /// </summary>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        private void getGlobalAudioFunc(int count = 20, int offset = 0, string query="")
        {
            searchUserAudio = false;
            XmlNodeList nodeList;
            //item.Clear();
            currentAudioList = new List<audioItems>();
            if (isAuthorised)
            {
                XmlDocument audioXml;
                audioXml = vkApiClass.getAudioGlobal(userId, count, offset,query);
                nodeList = audioXml.SelectNodes("response/audio");
                for (int i = 0; i < nodeList.Count; i++)
                {
                    audioItems ite = new audioItems();
                    ite.title = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("title"));
                    ite.artist = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("artist"));
                    ite.duration = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("duration"));
                    ite.source = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("url"));
                    currentAudioList.Add(ite);
                }
                dataGridView1.ItemsSource = currentAudioList;

            }
            else MessageBox.Show("Сначала нужно авторизоваться", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void button_moreOffset_Click(object sender, EventArgs e)
        {
            currentOffset+=N;
            if (searchUserAudio)
            {
                getAudioFunc(N, currentOffset);
            }
            else
            {
                getGlobalAudioFunc(N,currentOffset,textBox_searchGlobalAudio.Text);
            }
        }

        private void button_lessOffset_Click(object sender, EventArgs e)
        {
            if(currentOffset>10)
            currentOffset -= N;
            if (searchUserAudio)
            {
                getAudioFunc(N, currentOffset);
            }
            else
            {
                getGlobalAudioFunc(N, currentOffset, textBox_searchGlobalAudio.Text);
            }
        }

        string downloadAdress;
        bool downloading = false;
        private void button_downloadAudio_Click(object sender, RoutedEventArgs e)
        { 
            if (isAuthorised)
            {
            //audioToDownload.Clear();           
            System.Windows.Forms.FolderBrowserDialog fb = new System.Windows.Forms.FolderBrowserDialog();
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK) //Если пользователь не выберет папку, загрузка не начнется
            {
                downloadAdress = fb.SelectedPath;
            
            for (int i = audioToDownload.Count; i < currentAudioList.Count; i++)
            {
                if (currentAudioList[i].audioChecked)
                {
                    audioItems a = currentAudioList[i];
                    audioToDownload.Add(currentAudioList[i]);//Составляем очередь загрузки
                }
            }
            if (audioToDownload.Count != 0&&!downloading)
            {
                downloading = true;
                label_downloadingFileName.Content = audioToDownload[0].artist + " - " + audioToDownload[0].title;
                client.DownloadFileAsync(new Uri(audioToDownload[0].source), downloadAdress + "\\" + audioToDownload[0].artist + " - " + audioToDownload[0].title + ".mp3");
            }
            
            }
            }
            else MessageBox.Show("Сначала нужно авторизоваться", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            audioToDownload.RemoveAt(0);
            if (audioToDownload.Count != 0)
            {
                label_downloadingFileName.Content = audioToDownload[0].artist + " - " + audioToDownload[0].title;
                client.DownloadFileAsync(new Uri(audioToDownload[0].source), downloadAdress + "\\" + audioToDownload[0].artist + " - " + audioToDownload[0].title + ".mp3");

            }
            else
            {
                progressBar_download.Value = 0;
                downloading = false;
            }
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar_download.Value = e.ProgressPercentage;
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            playSelectedAudio();
        }

        private void playSelectedAudio()
        {
            mediaPlayer.Source = new Uri(currentAudioList[dataGridView1.SelectedIndex].source + ".mp3");
            mediaPlayer.Play();
        }

        private void ButtonPause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
        }

    }
}
