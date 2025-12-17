using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace General_PCR18.UControl
{
    /// <summary>
    /// Interaction logic for LoadingWait.xaml
    /// </summary>
    public partial class LoadingWait : UserControl
    {
        private Storyboard board = null;

        public LoadingWait()
        {
            InitializeComponent();

            this.Loaded += Windows_Loaded;
        }

        private void Windows_Loaded(object sender, RoutedEventArgs e)
        {
            ShowGifByAnimate(@"pack://application:,,,/Images/Loading.gif");
        }

        /// <summary>
        /// 显示GIF动图
        /// </summary>
        private void ShowGifByAnimate(string filePath)
        {
            this.Dispatcher.Invoke(() =>
            {
                List<BitmapFrame> frameList = new List<BitmapFrame>();
                GifBitmapDecoder decoder = new GifBitmapDecoder(
                new Uri(filePath, UriKind.RelativeOrAbsolute),
                BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                if (decoder != null && decoder.Frames != null)
                {
                    frameList.AddRange(decoder.Frames);
                    ObjectAnimationUsingKeyFrames objKeyAnimate = new ObjectAnimationUsingKeyFrames();
                    objKeyAnimate.Duration = new Duration(TimeSpan.FromSeconds(1));
                    foreach (var item in frameList)
                    {
                        DiscreteObjectKeyFrame k1_img1 = new DiscreteObjectKeyFrame(item);
                        objKeyAnimate.KeyFrames.Add(k1_img1);
                    }
                    imgGifShow.Source = frameList[0];

                    board = new Storyboard();
                    board.RepeatBehavior = RepeatBehavior.Forever;
                    board.FillBehavior = FillBehavior.HoldEnd;
                    board.Children.Add(objKeyAnimate);
                    Storyboard.SetTarget(objKeyAnimate, imgGifShow);
                    Storyboard.SetTargetProperty(objKeyAnimate, new PropertyPath("(Image.Source)"));
                    board.Begin();
                }
            });
        }
    }
}
