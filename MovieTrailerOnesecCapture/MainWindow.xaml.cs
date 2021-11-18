using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
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


namespace MovieTrailerOnesecCapture
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public class RGB
        {
            public float R { get; }
            public float G { get; }
            public float B { get; }
            public RGB(float b, float g, float r)
            {
                R = r;
                G = g;
                B = b;
            }
        }

        public class Color
        {
            public string movie { get; set; }
            public string color { get; set; }
            public string color_url { get; set; }
            public int color_1_R { get; set; }
            public int color_1_G { get; set; }
            public int color_1_B { get; set; }
            public int color_2_R { get; set; }
            public int color_2_G { get; set; }
            public int color_2_B { get; set; }
            public int color_3_R { get; set; }
            public int color_3_G { get; set; }
            public int color_3_B { get; set; }
            public int color_4_R { get; set; }
            public int color_4_G { get; set; }
            public int color_4_B { get; set; }
            public int color_5_R { get; set; }
            public int color_5_G { get; set; }
            public int color_5_B { get; set; }
        }
            private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = "video&image";
            openFile.Filter = "video&image|*.mov;*.mp4;*.png;";

            if (openFile.ShowDialog() == true)
            {
                VideoCapture video = new VideoCapture(openFile.FileName);
                int length = video.FrameCount;
                int clustersCount = 5;
                int total_x = 1920;
                List<RGB> RGB_List = new List<RGB>();

                using (Mat src = new Mat(),
                    samples = new Mat(),
                    bestLabels = new Mat(),
                    centers = new Mat())
                {
                    int cnt = 0;
                    while (true)
                    {
                        video.Read(src);

                        if (src.Empty())
                            break;

                        //double Frame_msec = video.PosMsec;
                        //Console.WriteLine(Frame_msec);

                        Cv2.Blur(src, src, new OpenCvSharp.Size(15, 15));
                        var columnVector = src.Reshape(cn: 3, rows: src.Rows * src.Cols);
                        columnVector.ConvertTo(samples, MatType.CV_32FC3);

                        Cv2.Kmeans(
                            data: samples,
                            k: clustersCount,
                            bestLabels: bestLabels,
                            criteria:
                                new TermCriteria(type: CriteriaTypes.Eps | CriteriaTypes.MaxIter, maxCount: 10, epsilon: 1.0),
                            attempts: 3,
                            flags: KMeansFlags.PpCenters,
                            centers: centers);

                        for (int idx = 0; idx < 5; idx++) 
                        {
                            RGB RGB_row = new RGB(centers.At<float>(idx, 0), centers.At<float>(idx, 1), centers.At<float>(idx, 2));
                            RGB_List.Add(RGB_row);
                        }
                        columnVector.Dispose();
                        Console.WriteLine(cnt++ + "/" + length);
                    }
                    // -Todo-
                    // RGB_List에서 뽑은 색들을 차례대로 이미지에 넣는다
                    // col은 당연히 RGB_List의 len
                    // row는 대충 50~100정도 사이즈, 이쁜걸로
                    // 각각의 col 채널에 접근하여 색 입히기
                    // 그 후 이미지로 변환하여 저장
                    int total_y = RGB_List.Count / total_x;
                    Mat result = new Mat(50 * (total_y + 1), total_x, MatType.CV_8UC3, new Scalar(0, 0, 0));
                    for (int col = 0; col < RGB_List.Count; col++)
                    {
                        int y = col / total_x;
                        int x = col % total_x;
                        var newPixel = new Vec3b
                        {
                            Item0 = (byte)RGB_List[col].B, // B
                            Item1 = (byte)RGB_List[col].G, // G
                            Item2 = (byte)RGB_List[col].R // R
                        };
                        for (int row = 0; row < 50; row++)
                        {
                            result.Set(50 * y + row, x, newPixel);
                        }
                        Console.WriteLine(col + "/" + RGB_List.Count);
                    }

                    int lastIndex = openFile.FileName.LastIndexOf('.');
                    string filename = openFile.FileName.Substring(0, lastIndex) + ".png";
                    Cv2.ImWrite(filename, result);
                    MessageBox.Show("이미지 추출 완료");
                    textBox.Text = "Done.";
                    result.Dispose();
                }
                video.Dispose();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.DefaultExt = "png";
            openFile.Filter = "png|*.png;";
            textBox.Text = "선택하시오";
            if (openFile.ShowDialog() == true)
            {
                using (Mat samples = new Mat(),
                    bestLabels = new Mat(),
                    centers = new Mat())
                {
                    Mat result = Cv2.ImRead(openFile.FileName);
                    var TotalcolumnVector = result.Reshape(cn: 3, rows: result.Rows * result.Cols);
                    TotalcolumnVector.ConvertTo(samples, MatType.CV_32FC3);

                    Cv2.Kmeans(
                            data: samples,
                            k: 6,
                            bestLabels: bestLabels,
                            criteria:
                                new TermCriteria(type: CriteriaTypes.Eps | CriteriaTypes.MaxIter, maxCount: 10, epsilon: 1.0),
                            attempts: 3,
                            flags: KMeansFlags.PpCenters,
                            centers: centers);
                    
                    int[,] total_color = new int[6, 3];

                    int darkest_index = 0;
                    int darkest_value = 255 * 3;
                    for (int i = 0; i < 6; i++)
                    {
                        total_color[i, 0] = (int)centers.At<float>(i, 0);
                        total_color[i, 1] = (int)centers.At<float>(i, 1);
                        total_color[i, 2] = (int)centers.At<float>(i, 2);

                        if (darkest_value > total_color[i, 0] + total_color[i, 1] + total_color[i, 2])
                        {
                            darkest_value = total_color[i, 0] + total_color[i, 1] + total_color[i, 2];
                            darkest_index = i;
                        }
                        //tex += i + ": R:" + total_R[i] + "\nG: " + total_G[i] + "\nB:" + total_B[i] + "\n";
                    }

                    total_color[darkest_index, 0] = total_color[5, 0];
                    total_color[darkest_index, 1] = total_color[5, 1];
                    total_color[darkest_index, 2] = total_color[5, 2];

                    string filePath = @"D:\movie\movie.json";

                    //// Json 파일 읽기
                    var jsonData = System.IO.File.ReadAllText(filePath);
                    // De-serialize to object or create new list
                    var colorList = JsonConvert.DeserializeObject<List<Color>>(jsonData)
                                          ?? new List<Color>();

                    //Add any new employees
                    colorList.Add(new Color()
                    {
                        movie = openFile.FileName,
                        color_1_B = total_color[0, 0],
                        color_1_G = total_color[0, 1],
                        color_1_R = total_color[0, 2],
                        color_2_B = total_color[1, 0],
                        color_2_G = total_color[1, 1],
                        color_2_R = total_color[1, 2],
                        color_3_B = total_color[2, 0],
                        color_3_G = total_color[2, 1],
                        color_3_R = total_color[2, 2],
                        color_4_B = total_color[3, 0],
                        color_4_G = total_color[3, 1],
                        color_4_R = total_color[3, 2],
                        color_5_B = total_color[4, 0],
                        color_5_G = total_color[4, 1],
                        color_5_R = total_color[4, 2],
                    });

                    //Update json data string
                   jsonData = JsonConvert.SerializeObject(colorList);
                    System.IO.File.WriteAllText(filePath, jsonData);

                    result.Dispose();
                    TotalcolumnVector.Dispose();
                    textBox.Text = "Done.";
                }
            }
        }
    }
}
