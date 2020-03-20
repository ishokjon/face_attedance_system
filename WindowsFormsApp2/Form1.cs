﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6d, 0.6d);
        HaarCascade faceDetected;
        Image<Bgr, Byte> Frame;
        Capture camera;
        Image<Gray, byte> result;
        Image<Gray, byte> TrainedFace = null;
        Image<Gray, byte> grayFace = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> Users = new List<string>();
        int Count, NumLables, t;
        string name, names = null;
        public static string connectString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=baza.mdb;";
        private OleDbConnection myConnection;

        public Form1()
        {
            InitializeComponent();
            faceDetected = new HaarCascade("haarcascade_frontalface_default.xml");
            myConnection = new OleDbConnection(connectString);
            myConnection.Open();
            MessageBox.Show(myConnection.State.ToString());
            try
            {
                string Labelsinf = File.ReadAllText(Application.StartupPath + "/Faces/Faces.txt");
                string[] Labels = Labelsinf.Split(',');
                //The first Label before, willl be the number of faces saved.
                NumLables = Convert.ToInt16(Labels[0]);
                Count = NumLables;
                string FacesLoad;
                for (int i = 1; i < NumLables + 1; i++)
                {
                    FacesLoad = "face" + i + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + $"/Faces/{FacesLoad}"));
                    labels.Add(Labels[i]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bazada ma'lumot  mavjud emas!");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            myConnection.Close();
        }

        private void start_Click(object sender, EventArgs e)
        {
            camera = new Capture();
            camera.QueryFrame();
            Application.Idle += new EventHandler(FrameProcedure);
        }
        private void saveButton_Click(object sender, EventArgs e)
        {
            Count = Count + 1;
            grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            MCvAvgComp[][] DetectedFaces = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            foreach(MCvAvgComp f in DetectedFaces[0])
            {
                TrainedFace = Frame.Copy(f.rect).Convert<Gray, byte>();
                break;
            }
            TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            trainingImages.Add(TrainedFace);
            labels.Add(textName.Text);
            File.WriteAllText(Application.StartupPath + "/Faces/faces.txt", trainingImages.ToArray().Length.ToString() + ",");
            for(int i=1; i<trainingImages.ToArray().Length+1;i++)
            {
                trainingImages.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face" + i + ".bmp");
                File.AppendAllText(Application.StartupPath + "/Faces/faces.txt", labels.ToArray()[i - 1] + ",");

            }

            string query = "INSERT INTO talaba (ism, fam, guvohnoma,guruh,fakultet,yonalish,telefon) VALUES ('"+i.Text+"', '"+ f.Text + "', '"+textName.Text+"', '"+g.Text+"', '"+fac.Text+"', '"+y.Text+"', '"+tel.Text+"')";

            // создаем объект OleDbCommand для выполнения запроса к БД MS Access
            OleDbCommand command = new OleDbCommand(query, myConnection);

            // выполняем запрос к MS Access
            command.ExecuteNonQuery();


            MessageBox.Show("Muvaffaqiyatli qo'shildi!");
        }
        private void FrameProcedure(object sender, EventArgs e)
        {
            Users.Add("");
            Frame = camera.QueryFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayFace = Frame.Convert<Gray, Byte>();
            MCvAvgComp[][] facesDetectedNow = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            foreach(MCvAvgComp f in facesDetectedNow[0])
            {
                result = Frame.Copy(f.rect).Convert<Gray, Byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC) ;
                Frame.Draw(f.rect, new Bgr(Color.Green),3);
                if(trainingImages.ToArray().Length!=0)
                {
                    MCvTermCriteria termCriterias = new MCvTermCriteria(Count, 0.00001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), labels.ToArray(), 1500, ref termCriterias);
                    name = recognizer.Recognize(result);
                    fish.Text = name;
                    
                    if (name!="" && name!=".")
                    {
                        string query = "SELECT * FROM talaba WHERE guvohnoma = '" + name + "'";
                                         
                        // создаем объект OleDbCommand для выполнения запроса к БД MS Access
                        OleDbCommand command = new OleDbCommand(query, myConnection);

                        // получаем объект OleDbDataReader для чтения табличного результата запроса SELECT
                        OleDbDataReader reader = command.ExecuteReader();

                        // в цикле построчно читаем ответ от БД
                        while (reader.Read())
                        {
                            // выводим данные столбцов текущей строки в listBox1
                            ism.Text = reader[1].ToString();
                            fam.Text = reader[2].ToString();
                            yon.Text = reader[5].ToString();
                            guruh.Text = reader[4].ToString();

                        }

                        // закрываем OleDbDataReader
                        reader.Close();
                    }

                    Frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Red));

                }
                
                Users.Add("");
            }
            cameraBox.Image = Frame;
            names = "";
            Users.Clear();
        } 
    }
}
