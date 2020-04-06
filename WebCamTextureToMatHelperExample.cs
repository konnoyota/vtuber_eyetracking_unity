#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using DlibFaceLandmarkDetector;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Linq; //追加しました。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DlibFaceLandmarkDetectorExample
{
    /// <summary>
    /// WebCamTextureToMatHelper Example
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class WebCamTextureToMatHelperExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The dlib shape predictor file name.
        /// </summary>
        string dlibShapePredictorFileName = "sp_human_face_68.dat";

        /// <summary>
        /// The dlib shape predictor file path.
        /// </summary>
        string dlibShapePredictorFilePath;

#if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            dlibShapePredictorFileName = DlibFaceLandmarkDetectorExample.dlibShapePredictorFileName;
#if UNITY_WEBGL && !UNITY_EDITOR
            getFilePath_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync (dlibShapePredictorFileName, (result) => {
                getFilePath_Coroutine = null;

                dlibShapePredictorFilePath = result;
                Run ();
            });
            StartCoroutine (getFilePath_Coroutine);
#else
            dlibShapePredictorFilePath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath(dlibShapePredictorFileName);
            Run();
#endif
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(dlibShapePredictorFilePath))
            {
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(dlibShapePredictorFilePath);

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("dlib shape predictor", dlibShapePredictorFileName);
                fpsMonitor.Add("width", webCamTextureToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", webCamTextureToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                OpenCVForUnityUtils.SetImage(faceLandmarkDetector, rgbaMat);

                //detect face rects
                List<UnityEngine.Rect> detectResult = faceLandmarkDetector.Detect();

                foreach (var rect in detectResult)
                {

                    //detect landmark points
                    List<Vector2> points = faceLandmarkDetector.DetectLandmark(rect);

                    //draw landmark points
                    OpenCVForUnityUtils.DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                    //draw face rect
                    OpenCVForUnityUtils.DrawFaceRect(rgbaMat, rect, new Scalar(255, 0, 0, 255), 2);
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D(rgbaMat, texture);
            }
        }
        //ここに追加していく。
        private void live2DModelUpdate(List<Vector2> points)
       {

           if (live2DModel != null) {

               //angle
               Vector3 angles = getFaceAngle(points);
               float rotateX = (angles.x > 180) ? angles.x - 360 : angles.x;
               float rotateY = (angles.y > 180) ? angles.y - 360 : angles.y;
               float rotateZ = (angles.z > 180) ? angles.z - 360 : angles.z;
               live2DModel.PARAM_ANGLE.Set(-rotateY, rotateX, -rotateZ);//座標系を変換して渡す


               //eye_open_L
               float eyeOpen_L = getRaitoOfEyeOpen_L(points);
               Debug.Log(eyeOpen_L);
               if (eyeOpen_L > 0.8f && eyeOpen_L < 1.1f) eyeOpen_L = 1;
               if (eyeOpen_L >= 1.1f) eyeOpen_L = 2;
               if (eyeOpen_L < 0.7f) eyeOpen_L = 0;
               live2DModel.PARAM_EYE_L_OPEN = eyeOpen_L;

               //eye_open_R
               float eyeOpen_R = getRaitoOfEyeOpen_R(points);
               if (eyeOpen_R > 0.8f && eyeOpen_R < 1.1f) eyeOpen_R = 1;
               if (eyeOpen_R >= 1.1f) eyeOpen_R = 2;
               if (eyeOpen_R < 0.7f) eyeOpen_R = 0;
               live2DModel.PARAM_EYE_R_OPEN = eyeOpen_R;

               //eye_ball_X
               live2DModel.PARAM_EYE_BALL_X = rotateY / 60f;//視線が必ずカメラ方向を向くようにする
               //eye_ball_Y
               live2DModel.PARAM_EYE_BALL_Y = -rotateX / 60f - 0.25f;//視線が必ずカメラ方向を向くようにする

               //brow_L_Y
               float brow_L_Y = getRaitoOfBROW_L_Y(points);
               live2DModel.PARAM_BROW_L_Y = brow_L_Y;

               //brow_R_Y
               float brow_R_Y = getRaitoOfBROW_R_Y(points);
               live2DModel.PARAM_BROW_R_Y = brow_R_Y;

               //mouth_open
               float mouthOpen = getRaitoOfMouthOpen_Y(points) * 2f;
               if (mouthOpen < 0.3f) mouthOpen = 0;
               live2DModel.PARAM_MOUTH_OPEN_Y = mouthOpen;

               //mouth_size
               float mouthSize = getRaitoOfMouthSize(points);
               live2DModel.PARAM_MOUTH_SIZE = mouthSize;

           }
       }

       //目の開き具合を算出
       private float getRaitoOfEyeOpen_L(List<Vector2> points)
       {
           if (points.Count != 68)
               throw new ArgumentNullException("Invalid landmark_points.");

           return Mathf.Clamp(Mathf.Abs(points[43].y - points[47].y) / (Mathf.Abs(points[43].x - points[44].x) * 0.75f), -0.1f, 2.0f);
       }

       private float getRaitoOfEyeOpen_R(List<Vector2> points)
       {
           if (points.Count != 68)
               throw new ArgumentNullException("Invalid landmark_points.");

           return Mathf.Clamp(Mathf.Abs(points[38].y - points[40].y) / (Mathf.Abs(points[37].x - points[38].x) * 0.75f), -0.1f, 2.0f);
       }

       //眉の上下
       private float getRaitoOfBROW_L_Y(List<Vector2> points)
       {
           if (points.Count != 68)
               throw new ArgumentNullException("Invalid landmark_points.");

           float y = Mathf.Abs(points[24].y - points[27].y) / Mathf.Abs(points[27].y - points[29].y);
           y -= 1;
           y *= 4f;

           return Mathf.Clamp(y, -1.0f, 1.0f);
       }

       private float getRaitoOfBROW_R_Y(List<Vector2> points)
       {
           if (points.Count != 68)
               throw new ArgumentNullException("Invalid landmark_points.");

           float y = Mathf.Abs(points[19].y - points[27].y) / Mathf.Abs(points[27].y - points[29].y);
           y -= 1;
           y *= 4f;

           return Mathf.Clamp(y, -1.0f, 1.0f);
       }

       //口の開き具合を算出
       private float getRaitoOfMouthOpen_Y(List<Vector2> points)
       {
           if (points.Count != 68)
               throw new ArgumentNullException("Invalid landmark_points.");

           return Mathf.Clamp01(Mathf.Abs(points[62].y - points[66].y) / (Mathf.Abs(points[51].y - points[62].y) + Mathf.Abs(points[66].y - points[57].y)));
       }

       //口の幅サイズを算出
       private float getRaitoOfMouthSize(List<Vector2> points)
       {
           if (points.Count != 68)
               throw new ArgumentNullException("Invalid landmark_points.");

           float size = Mathf.Abs(points[48].x - points[54].x) / (Mathf.Abs(points[31].x - points[35].x) * 1.8f);
           size -= 1;
           size *= 4f;

           return Mathf.Clamp(size, -1.0f, 1.0f);
       }
       //参考ページhttps://cppx.hatenablog.com/entry/2017/12/25/231121#get_center　pythonだけど・・・
       private float getEyePoint(points,left = true)
       {
        int[] lefteye = new int[6] {points[36],points[37],points[38],points[39],points[40],points[41]};
        int[] rigtheye = new int[6] {points[42],points[43],points[44],points[45],points[46],points[47]};

        //画像から見えている座標を選定する。
      　int lefteye1  = lefteye[2](x => x.y)); //points[37]
        int lefteye2  = lefteye[3](x => x.y)); //points[38]
        int eyemin = Mathf.min(lefteye1,lefteye2);

        int lefteye3  = lefteye[4](x => x.y)); //points[37]
        int lefteye4  = lefteye[5](x => x.y)); //points[38]
        int eyemax =  Mathf.max(lefteye3,lefteye4);

        //視点移動量は両目同じと想定（ばらばらに瞳動かせる特殊な人はそうそういませんよね）
        org_x = eyes[0].x //x軸
        org_y = eyes[1].y //y軸
        //目が閉じていたら処理完了
        if is_close(org_y, eyes[2].y):
          return None
        //二値化はいらない？//
       }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose();

#if UNITY_WEBGL && !UNITY_EDITOR
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
            }
#endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("DlibFaceLandmarkDetectorExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonCkick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }
    }
}

#endif
