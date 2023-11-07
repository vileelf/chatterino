using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Security;

//Wrapper class to handle various image functions for Chatterino including webp support.
namespace Chatterino.Common {
    public class ChatterinoImage {
        public Image ActiveImage;
        private MemoryStream OriginalImageStream;
        private Image OriginalImage = null;
        private List<Image> Frames;
        public int TotalFrames;
        public int CurrentFrame;
        private List<int> FrameDurations;
        public bool IsAnimated;
        public int Height;
        public int Width;
        private bool framesLoaded = true;
        public bool UsedLastCycle = true;
        public bool IsLoaded;

        public ChatterinoImage(MemoryStream stream) {
            OriginalImageStream = stream;
            Frames = new List<Image>();
            FrameDurations = new List<int>();
            loadImageFromStream(stream);
        }

        //this constructor doesnt support using the original stream for saving purposes. only can call image.save
        public ChatterinoImage(Image image) {
            OriginalImageStream = null;
            OriginalImage = (Image)image.Clone();
            Frames = new List<Image>();
            FrameDurations = new List<int>();
            ActiveImage = image;
            lock (ActiveImage) {
                Width = image.Width;
                Height = image.Height;
            }
            LoadImageFrameInfo();
            IsLoaded = true;
        }

        public ChatterinoImage(Stream stream) {
            MemoryStream memstream = new MemoryStream();
            stream.CopyTo(memstream);
            OriginalImageStream = memstream;
            Frames = new List<Image>();
            FrameDurations = new List<int>();
            loadImageFromStream(memstream);
        }

        public ChatterinoImage(string filename) {
            Stream filestream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            MemoryStream stream = new MemoryStream();
            filestream.CopyTo(stream);
            filestream.Close();
            OriginalImageStream = stream;
            Frames = new List<Image>();
            FrameDurations = new List<int>();
            loadImageFromStream(stream);
        }

        /// <summary>
        /// Creates a clone of the class (not an exact copy)
        /// </summary>
        /// <returns>a clone of this class</returns>
        public ChatterinoImage Clone() {
            if (OriginalImageStream != null) {
                MemoryStream memstream = new MemoryStream();
                OriginalImageStream.CopyTo(memstream);
                return new ChatterinoImage(memstream);
            } else {
                return new ChatterinoImage((Image)OriginalImage.Clone());
            }
        }

        //unloads the image and its frames and frees up the memory. Only done if the original stream is preserved
        public void UnloadImage() {
            if (OriginalImageStream != null) {
                ActiveImage?.Dispose();
                foreach (var frame in Frames) {
                    frame.Dispose();
                }
                Frames.Clear();
                FrameDurations.Clear();
                ActiveImage = null;
                IsLoaded = false;
            }
        }

        //reload the image after its been unloaded. 
        public void ReloadImage() {
            if (!IsLoaded && OriginalImageStream != null) {
                loadImageFromStream(OriginalImageStream);
                IsLoaded = true;
            }
        }

        private void loadImageFromStream(MemoryStream stream) {
            IsAnimated = false;
            if (!IsWebp(stream)) {
                ActiveImage = Image.FromStream(stream);
                lock (ActiveImage) {
                    Width = ActiveImage.Width;
                    Height = ActiveImage.Height;
                }
                LoadImageFrameInfo();
            } else {
                TotalFrames = 0;
                decodeWebP(stream);
            }
            IsLoaded = true;
        }

        private void LoadImageFrameInfo() {
            if (ImageAnimator.CanAnimate(ActiveImage)) {
                //extract the frames
                FrameDimension dimension = new FrameDimension(ActiveImage.FrameDimensionsList[0]);
                PropertyItem framedelayprop;
                lock (ActiveImage) {
                    TotalFrames = ActiveImage.GetFrameCount(dimension);
                    framedelayprop = ActiveImage.GetPropertyItem(0x5100);
                }
                Byte[] times = framedelayprop.Value;
                int framedelay = 0;
                for (int i = 0; i < TotalFrames; i++) {
                    framedelay = BitConverter.ToInt32(times, 4 * i);
                    onlyAddFrameDelay(framedelay);
                }
                IsAnimated = true;
                framesLoaded = false;
            }
        }

        private void LoadImageFrames() {
            FrameDimension dimension = new FrameDimension(ActiveImage.FrameDimensionsList[0]);
            lock (ActiveImage) {
                for (int i = 0; i < TotalFrames; i++) {
                    ActiveImage.SelectActiveFrame(dimension, i);
                    onlyAddFrame(ActiveImage);
                }
            }
            ActiveImage = Frames[CurrentFrame];
            framesLoaded = true;
        }

        private void onlyAddFrameDelay(int frameDuration) {
            FrameDurations.Add(frameDuration < 2 ? 10 : frameDuration);
        }

        private void onlyAddFrame(Image frame) {
            Image newframe = (Image)frame.Clone();
            Frames.Add(newframe);
        }

        private bool IsWebp(MemoryStream stream) {
            byte[] buffer = stream.ToArray();
            bool ret = false;
            //make sure we are at the start of the stream
            //stream.Seek(0, SeekOrigin.Begin);
            //the header should be 4 bytes for RIFF, 4 bytes for file size, and then 4 bytes for WEBP
            //0-3 = riff 4-7 = filesize 8-11 = webp
            //int count = stream.Read(buffer, 0, 12);
            if (buffer.Length > 12) {
                string webp;
                webp = "" + Convert.ToChar(buffer[8]) + Convert.ToChar(buffer[9]) + Convert.ToChar(buffer[10]) + Convert.ToChar(buffer[11]);
                if (string.Compare(webp.ToUpper(), "WEBP") == 0) {
                    ret = true;
                }
            }

            //seek back to the beginning
            //stream.Seek(0, SeekOrigin.Begin);
            return ret;
        }

        //Creates a ChatterinoImage object from the file given
        public static ChatterinoImage FromFile(string filename) {
            return new ChatterinoImage(filename);
        }

        //Creates a ChatterinoImage object from the stream given
        public static ChatterinoImage FromStream(Stream stream) {
            return new ChatterinoImage(stream);
        }

        public static ChatterinoImage FromStream(MemoryStream stream) {
            return new ChatterinoImage(stream);
        }

        //Sets the current active frame to the one indicated by frame index
        public void SelectActiveFrame(int frameIndex) {
            if (!IsLoaded) {
                ReloadImage();
            }
            UsedLastCycle = true;
            if (framesLoaded) {
                ActiveImage = Frames[frameIndex];
            } else {
                FrameDimension dimension = new FrameDimension(ActiveImage.FrameDimensionsList[0]);
                lock (ActiveImage) {
                    ActiveImage.SelectActiveFrame(dimension, frameIndex);
                }
            }
            lock (ActiveImage) {
                this.Height = ActiveImage.Height;
                this.Width = ActiveImage.Width;
            }
            this.CurrentFrame = frameIndex;
        }

        //Get the frame duration for frameIndex
        public int GetFrameDuration(int frameIndex) {
            if (!IsLoaded) {
                ReloadImage();
            }
            return FrameDurations[frameIndex];

        }

        //gets the frame at frameIndex
        public Image GetFrame(int frameIndex) {
            if (!IsLoaded) {
                ReloadImage();
            }
            if (!framesLoaded) {
                LoadImageFrames();
            }
            UsedLastCycle = true;
            return Frames[frameIndex];
        }

        //gets the total count of frames
        public int GetFrameCount() {
            return TotalFrames;
        }

        //Creates a copy and adds a frame to the list. Uses the same frame duration as the previous frame. If no previous frame defaults to 10 centiseconds.
        public void AddFrame(Image frame) {
            AddFrame(frame, FrameDurations[FrameDurations.Count > 0 ? FrameDurations.Count - 1 : 10]);
        }

        //Creates a copy and adds a frame to the list. Frame duration is in centiseconds. If frame duration is less than 2 defaults it to 10. 
        public void AddFrame(Image frame, int frameDuration) {
            if (!framesLoaded) {
                LoadImageFrames();
            }
            Image newframe = (Image)frame.Clone();
            Frames.Add(newframe);
            FrameDurations.Add(frameDuration < 2 ? 10 : frameDuration);
            TotalFrames++;
        }

        //Saves the original stream to the file (does not save added frames)
        public void Save(string filename) {
            Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
            this.Save(stream);
        }

        //Saves the original stream to the stream (does not save added frames, if no original stream uses Image.Save method)
        public void Save(Stream stream) {
            if (false) {
                OriginalImageStream.CopyTo(stream);
                stream.Flush();
            } else if (!IsAnimated) {
                lock (ActiveImage) {
                    ActiveImage.Save(stream, GetEncoder(ImageFormat.Png), null);
                }
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format) {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs) {
                if (codec.FormatID == format.Guid) {
                    return codec;
                }
            }
            return null;
        }

        //Draws the current active frame
        public void DrawImage(Graphics g, int x, int y) {
            if (!IsLoaded) {
                ReloadImage();
            }
            lock (ActiveImage) {
                UsedLastCycle = true;
                g.DrawImage(ActiveImage, x, y);
            }
        }

        //Draws the current active frame
        public void DrawImage(Graphics g, int x, int y, int width, int height) {
            if (!IsLoaded) {
                ReloadImage();
            }
            lock (ActiveImage) {
                UsedLastCycle = true;
                g.DrawImage(ActiveImage, x, y, width, height);
            }
        }

        private void decodeWebP(MemoryStream stream) {
            Bitmap bmp = null;
            Bitmap firstbmp = null;
            bool firstbitmap = true;
            BitmapData bmpData = null;
            IntPtr dec = (IntPtr)0;
            const int UintBytes = 4;
            byte[] rawWebP = stream.ToArray();
            UIntPtr dataSize = (UIntPtr)rawWebP.Length;
            GCHandle pinnedWebP = GCHandle.Alloc(rawWebP, GCHandleType.Pinned);
            IntPtr ptrData = pinnedWebP.AddrOfPinnedObject();
            try {
                //Get info about webp image
                GetWebpInfo(ptrData, rawWebP.Length, out int imgWidth, out int imgHeight, out bool hasAlpha, out bool hasAnimation, out string format);

                if (hasAnimation) {
                    //demux the image
                    WebPData webpdata = new WebPData() { bytes = ptrData, size = dataSize };
                    WebPAnimDecoderOptions dec_options = new WebPAnimDecoderOptions();
                    //init decoder options with default values
                    WebPWrapper.WebPAnimDecoderOptionsInit(ref dec_options);
                    //set color mode to blue green red alpha to match up with how the bitmap is formatted
                    dec_options.color_mode = WEBP_CSP_MODE.MODE_BGRA;
                    //create the decoder
                    dec = WebPWrapper.WebPAnimDecoderNew(ref webpdata, ref dec_options);
                    IntPtr buf;
                    int timestamp;
                    uint outputSize;
                    WebPAnimInfo animinfo = new WebPAnimInfo();
                    //get animation info
                    WebPWrapper.WebPAnimDecoderGetInfo(dec, ref animinfo);
                    imgWidth = (int)animinfo.canvas_width;
                    imgHeight = (int)animinfo.canvas_height;
                    int prevtimestamp = 0;
                    bool first = true;
                    while (WebPWrapper.WebPAnimDecoderHasMoreFrames(dec) == 1) {
                        //get next frame
                        WebPWrapper.WebPAnimDecoderGetNext(dec, out buf, out timestamp);
                        //copy frame into an Image (Bitmap)
                        bmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format32bppArgb);
                        bmpData = bmp.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
                        outputSize = (uint)(bmpData.Stride * imgHeight);
                        WebPWrapper.CopyMemory(bmpData.Scan0, buf, outputSize);
                        bmp.UnlockBits(bmpData);
                        //Add frame to the list
                        AddFrame(bmp, (timestamp - prevtimestamp) / 10);
                        prevtimestamp = timestamp;
                        bmpData = null;
                        if (first) {
                            ActiveImage = bmp;
                            Height = imgHeight;
                            Width = imgWidth;
                            first = false;
                        }
                    }
                    WebPWrapper.WebPAnimDecoderReset(dec);
                    WebPWrapper.WebPAnimDecoderDelete(dec);
                    dec = (IntPtr)0;
                    IsAnimated = true;
                } else {
                    //Create a Bitmap and Lock all pixels to be written
                    if (hasAlpha)
                        bmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format32bppArgb);
                    else
                        bmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format24bppRgb);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);

                    //Uncompress the image
                    int outputSize = bmpData.Stride * imgHeight;
                    if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
                        WebPWrapper.WebPDecodeBGRInto(ptrData, rawWebP.Length, bmpData.Scan0, outputSize, bmpData.Stride);
                    else
                        WebPWrapper.WebPDecodeBGRAInto(ptrData, rawWebP.Length, bmpData.Scan0, outputSize, bmpData.Stride);

                    ActiveImage = bmp;
                    Height = imgHeight;
                    Width = imgWidth;
                }
            } catch (Exception e) {
                GuiEngine.Current.log(e.ToString());
            } finally {
                //Unlock the pixels
                if (bmpData != null) {
                    bmp.UnlockBits(bmpData);
                }

                if (dec != (IntPtr)0) {
                    WebPWrapper.WebPAnimDecoderDelete(dec);
                }
                //Free memory
                if (pinnedWebP.IsAllocated)
                    pinnedWebP.Free();
            }

        }

        private void GetWebpInfo(IntPtr ptrRawWebP, int webpDataSize, out int width, out int height, out bool has_alpha, out bool has_animation, out string format) {
            VP8StatusCode result;
            WebPBitstreamFeatures features = new WebPBitstreamFeatures();
            result = WebPWrapper.WebPGetFeatures(ptrRawWebP, webpDataSize, ref features);
            if (result == 0) {
                width = features.Width;
                height = features.Height;
                has_alpha = (features.Has_alpha == 1);
                has_animation = (features.Has_animation == 1);
                switch (features.Format) {
                    case 1:
                        format = "lossy";
                        break;
                    case 2:
                        format = "lossless";
                        break;
                    default:
                        format = "undefined";
                        break;
                }
            } else {
                throw new Exception(result.ToString());
            }
        }
    }
}