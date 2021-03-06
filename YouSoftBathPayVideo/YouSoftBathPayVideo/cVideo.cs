﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.IO;

//using AviFile;

namespace YouSoftBathPayVideo
{
    class cVideo
    {
        public bool flag = true;
        private IntPtr lwndC;       //保存无符号句柄
        private IntPtr mControlPtr; //保存管理指示器
        private int mWidth;
        private int mHeight;
        public delegate void RecievedFrameEventHandler(byte[] data);
        public event RecievedFrameEventHandler RecievedFrame;

        public VideoAPI.CAPTUREPARMS Capparms;
        private VideoAPI.FrameEventHandler mFrameEventHandler;
        public VideoAPI.CAPDRIVERCAPS CapDriverCAPS;//捕获驱动器的能力，如有无视频叠加能力、有无控制视频源、视频格式的对话框等；
        public VideoAPI.CAPSTATUS CapStatus;//该结构用于保存视频设备捕获窗口的当前状态，如图像的宽、高等
        string strFileName;
        public cVideo(IntPtr handle, int width, int height)
        {
            CapDriverCAPS = new VideoAPI.CAPDRIVERCAPS();//捕获驱动器的能力，如有无视频叠加能力、有无控制视频源、视频格式的对话框等；
            CapStatus = new VideoAPI.CAPSTATUS();//该结构用于保存视频设备捕获窗口的当前状态，如图像的宽、高等
            mControlPtr = handle; //显示视频控件的句柄
            mWidth = width;      //视频宽度
            mHeight = height;    //视频高度
        }
        /// <summary>
        /// 打开视频设备
        /// </summary>
        public bool StartWebCam()
        {
            //byte[] lpszName = new byte[100];
            //byte[] lpszVer = new byte[100];
            //VideoAPI.capGetDriverDescriptionA(0, lpszName, 100, lpszVer, 100);
            //this.lwndC = VideoAPI.capCreateCaptureWindowA(lpszName, VideoAPI.WS_CHILD | VideoAPI.WS_VISIBLE, 0, 0, mWidth, mHeight, mControlPtr, 0);
            //if (VideoAPI.SendMessage(lwndC, VideoAPI.WM_CAP_DRIVER_CONNECT, 0, 0))
            //{
            //    VideoAPI.SendMessage(lwndC, VideoAPI.WM_CAP_SET_PREVIEWRATE, 100, 0);
            //    VideoAPI.SendMessage(lwndC, VideoAPI.WM_CAP_SET_PREVIEW, true, 0);
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
            this.lwndC = VideoAPI.capCreateCaptureWindow("", VideoAPI.WS_CHILD | VideoAPI.WS_VISIBLE, 0, 0, mWidth, mHeight, mControlPtr, 0);//AVICap类的捕捉窗口
            VideoAPI.FrameEventHandler FrameEventHandler = new VideoAPI.FrameEventHandler(FrameCallback);
            VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_CALLBACK_ERROR, 0, 0);//注册错误回调函数
            VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_CALLBACK_STATUS, 0, 0);//注册状态回调函数 
            VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_CALLBACK_VIDEOSTREAM, 0, 0);//注册视频流回调函数
            VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_CALLBACK_FRAME, 0, FrameEventHandler);//注册帧回调函数

            //if (!CapDriverCAPS.fCaptureInitialized)//判断当前设备是否被其他设备连接已经连接
            //{

            if (VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_DRIVER_CONNECT, 0, 0))
            {
                //-----------------------------------------------------------------------
                VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_DRIVER_GET_CAPS, VideoAPI.SizeOf(CapDriverCAPS), ref CapDriverCAPS);//获得当前视频 CAPDRIVERCAPS定义了捕获驱动器的能力，如有无视频叠加能力、有无控制视频源、视频格式的对话框等；
                VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_GET_STATUS, VideoAPI.SizeOf(CapStatus), ref CapStatus);//获得当前视频流的尺寸 存入CapStatus结构

                VideoAPI.BITMAPINFO bitmapInfo = new VideoAPI.BITMAPINFO();//设置视频格式 (height and width in pixels, bits per frame). 
                bitmapInfo.bmiHeader = new VideoAPI.BITMAPINFOHEADER();
                bitmapInfo.bmiHeader.biSize = VideoAPI.SizeOf(bitmapInfo.bmiHeader);
                bitmapInfo.bmiHeader.biWidth = mWidth;
                bitmapInfo.bmiHeader.biHeight = mHeight;
                bitmapInfo.bmiHeader.biPlanes = 1;
                bitmapInfo.bmiHeader.biBitCount = 24;
                VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_PREVIEWRATE, 40, 0);//设置在PREVIEW模式下设定视频窗口的刷新率 设置每40毫秒显示一帧，即显示帧速为每秒25帧
                VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_SCALE, 1, 0);//打开预览视频的缩放比例
                VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_VIDEOFORMAT, VideoAPI.SizeOf(bitmapInfo), ref bitmapInfo);

                this.mFrameEventHandler = new VideoAPI.FrameEventHandler(FrameCallback);
                this.capSetCallbackOnFrame(this.lwndC, this.mFrameEventHandler);


                VideoAPI.CAPTUREPARMS captureparms = new VideoAPI.CAPTUREPARMS();
                VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_GET_SEQUENCE_SETUP, VideoAPI.SizeOf(captureparms), ref captureparms);
                if (CapDriverCAPS.fHasOverlay)
                {
                    VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_OVERLAY, 1, 0);//启用叠加 注：据说启用此项可以加快渲染速度    
                }
                VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_PREVIEW, 1, 0);//设置显示图像启动预览模式 PREVIEW
                VideoAPI.SetWindowPos(this.lwndC, 0, 0, 0, mWidth, mHeight, VideoAPI.SWP_NOZORDER | VideoAPI.SWP_NOMOVE);//使捕获窗口与进来的视频流尺寸保持一致
                return true;
            }
            else
            {

                flag = false;
                return false;
            }
        }
        
        
        public void get()
        {
            VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_GET_SEQUENCE_SETUP, VideoAPI.SizeOf(Capparms), ref Capparms);
        }
        public void set()
        {
            VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_SEQUENCE_SETUP, VideoAPI.SizeOf(Capparms), ref Capparms);
        }
        private bool capSetCallbackOnFrame(IntPtr lwnd, VideoAPI.FrameEventHandler lpProc)
        {
            return VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SET_CALLBACK_FRAME, 0, lpProc);
        }
        /// <summary>
        /// 关闭视频设备
        /// </summary>
        public void CloseWebcam()
        {
            VideoAPI.SendMessage(lwndC, VideoAPI.WM_CAP_DRIVER_DISCONNECT, 0, 0);
        }
        ///   <summary>   
        ///   拍照
        ///   </summary>   
        ///   <param   name="path">要保存bmp文件的路径</param>   
        public void GrabImage(IntPtr hWndC, string path)
        {
            IntPtr hBmp = Marshal.StringToHGlobalAnsi(path);
            VideoAPI.SendMessage(lwndC, VideoAPI.WM_CAP_SAVEDIB, 0, hBmp.ToInt32());
        }

        public void StartKinescope(string path)
        {
            strFileName = path;
            string dir = path.Remove(path.LastIndexOf("\\"));
            if (!File.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            int hBmp = Marshal.StringToHGlobalAnsi(path).ToInt32();
            bool b = VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_FILE_SET_CAPTURE_FILE, 0, hBmp);
            b = VideoAPI.SendMessage(this.lwndC, VideoAPI.WM_CAP_SEQUENCE, 0, 0);
        }
        /// <summary>
        /// 停止录像
        /// </summary>
        public void StopKinescope()
        {
            VideoAPI.SendMessage(lwndC, VideoAPI.WM_CAP_STOP, 0, 0);

            string fileName = strFileName.Substring(strFileName.LastIndexOf("\\"));
            File.Copy(strFileName, @"\\192.168.1.6\\videos\\" + fileName, true);
            Thread.Sleep(5000);
            File.Delete(strFileName);
            //Thread t = new Thread(new ThreadStart(CopyFile));
            //t.Start();
            //t.Join();
        }


        private void FrameCallback(IntPtr lwnd, IntPtr lpvhdr)
        {
            VideoAPI.VIDEOHDR videoHeader = new VideoAPI.VIDEOHDR();
            byte[] VideoData;
            videoHeader = (VideoAPI.VIDEOHDR)VideoAPI.GetStructure(lpvhdr, videoHeader);
            VideoData = new byte[videoHeader.dwBytesUsed];
            VideoAPI.Copy(videoHeader.lpData, VideoData);
            if (this.RecievedFrame != null)
                this.RecievedFrame(VideoData);
        }

        //压缩
        /*private void CopyFile()
        {
            try
            {
                //open compressed file
                Thread.CurrentThread.IsBackground = true;
                Thread.Sleep(10000);
                AviManager aviManager = new AviManager(strFileName, true);
                VideoStream aviStream = aviManager.GetVideoStream();
                //create un-/re-compressed file
                VideoStream newStream;

                string newFile = strFileName.Substring(0, strFileName.Length - 1);
                AviManager newManager = aviStream.DecompressToNewFile(newFile, true, out newStream);

                //close compressed file
                aviManager.Close();
                //save and close un-/re-compressed file
                newManager.Close();

                File.Delete(strFileName);
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                Thread.CurrentThread.Abort();
            }
        }*/
    }
}
