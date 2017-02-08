using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace WindowsFormsApplication5
{
    class SurfRansacMatcher
    {
        VectorOfKeyPoint modelKeyPoints = null;
        VectorOfKeyPoint observedKeyPoints = null;
        Matrix<int> indices = null;
        Matrix<byte> mask = null;

        public Matrix<float> selPoints1 = null;
        public Matrix<float> selPoints2 = null;

        //pointer to the feature point detector object
        private Feature2DBase<float> _Detector;
        //max ratio between 1st and 2nd NN
        private float _Ratio;
        //if true will refine the F matrix
        private bool _RefineF;
        //min distance to epipolar
        private double _Distance;
        //confidence level (probabillity)
        private double _Confidence;

        public SurfRansacMatcher()
        {
            this._Ratio = 0.55f;//0.60f;//0.55
            this._RefineF = true;
            this._Distance = 3.0;//3.0;
            this._Confidence = 0.99; //0.99
            this._Detector = new SURFDetector(100, false);//100
        }


        /// <summary>
        /// Match feature points using symmetry test and RANSAC
        /// </summary>
        /// <param name="image1">input image1</param>
        /// <param name="image2">input image2</param>
        /// <param name="keypoints1">output keypoint1</param>
        /// <param name="keypoints2">output keypoint2</param>
        /// <returns>return fundemental matrix</returns>
        public Image<Bgr, Byte> Match(Image<Gray, Byte> image1, Image<Gray, Byte> image2,
            ref VectorOfKeyPoint keypoints1, ref VectorOfKeyPoint keypoints2, bool computeModelFeatures)
        {
            //1a. Detection of the SURF features
            keypoints2 = null;
            if (computeModelFeatures == true)
                keypoints1 = this._Detector.DetectKeyPointsRaw(image1, null);
            keypoints2 = this._Detector.DetectKeyPointsRaw(image2, null);

            //1b. Extraction of the SURF descriptors
            Matrix<float> descriptors1 = this._Detector.ComputeDescriptorsRaw(image1, null, keypoints1);
            Matrix<float> descriptors2 = this._Detector.ComputeDescriptorsRaw(image2, null, keypoints2);

            //2. Match the two image descriptors
            //Construction of the match
            BruteForceMatcher<float> matcher = new BruteForceMatcher<float>(DistanceType.L2);
            //from image 1 to image 2
            //based on k nearest neighbours (with k=2)
            matcher.Add(descriptors1);
            //Number of nearest neighbors to search for
            int k = 2;
            int n = descriptors2.Rows;
            //The resulting n*k matrix of descriptor index from the training descriptors
            Matrix<int> trainIdx1 = new Matrix<int>(n, k);
            //The resulting n*k matrix of distance value from the training descriptors
            Matrix<float> distance1 = new Matrix<float>(n, k);
            matcher.KnnMatch(descriptors2, trainIdx1, distance1, k, null);
            matcher.Dispose();

            //from image 1 to image 2
            matcher = new BruteForceMatcher<float>(DistanceType.L2);
            matcher.Add(descriptors2);
            n = descriptors1.Rows;
            //The resulting n*k matrix of descriptor index from the training descriptors
            Matrix<int> trainIdx2 = new Matrix<int>(n, k);
            //The resulting n*k matrix of distance value from the training descriptors
            Matrix<float> distance2 = new Matrix<float>(n, k);
            matcher.KnnMatch(descriptors1, trainIdx2, distance2, k, null);

            //3. Remove matches for which NN ratio is > than threshold
            int removed = RatioTest(ref trainIdx1, ref distance1);
            removed = RatioTest(ref trainIdx2, ref distance2);

            //4. Create symmetrical matches
            Matrix<float> symMatches;
            int symNumber = SymmetryTest(trainIdx1, distance1, trainIdx2, distance2, out symMatches);

            //--------------modified code for zero matches------------
            if (symNumber == 0)  // no proper symmetrical matches, should retry in this case
                return null;
            //-----------------end modified code----------------------

            Matrix<double> fundementalMatrix = ApplyRANSAC(symMatches, keypoints1, keypoints2, symNumber);//, image2);

            //         Image<Bgr, Byte> resultImage = Features2DToolbox.DrawMatches(image1, modelKeyPoints, image2, observedKeyPoints,
            //indices, new Bgr(255, 0, 0), new Bgr(0, 255, 0), mask, Features2DToolbox.KeypointDrawType.DEFAULT);

            //         return resultImage;
            return null;   // we do our own drawing of correspondences
        }

        /// <summary>
        /// Clear matches for which NN ratio is > than threshold
        /// </summary>
        /// <param name="trainIdx">match descriptor index</param>
        /// <param name="distance">match distance value</param>
        /// <returns>return the number of removed points</returns>
        int RatioTest(ref Matrix<int> trainIdx, ref Matrix<float> distance)
        {
            int removed = 0;
            for (int i = 0; i < distance.Rows; i++)
            {
                if (distance[i, 0] / distance[i, 1] > this._Ratio)
                {
                    trainIdx[i, 0] = -1;  // -1 means, do not use this index
                    trainIdx[i, 1] = -1;
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// Create symMatches vector
        /// </summary>
        /// <param name="trainIdx1">match descriptor index 1</param>
        /// <param name="distance1">match distance value 1</param>
        /// <param name="trainIdx2">match descriptor index 2</param>
        /// <param name="distance2">match distance value 2</param>
        /// <param name="symMatches">return symMatches vector</param>
        /// <returns>return the number of symmetrical matches</returns>
        int SymmetryTest(Matrix<int> trainIdx1, Matrix<float> distance1, Matrix<int> trainIdx2, Matrix<float> distance2, out Matrix<float> symMatches)
        {
            symMatches = new Matrix<float>(trainIdx1.Rows, 4);
            int count = 0;
            //for all matches image1 -> image2
            for (int i = 0; i < trainIdx1.Rows; i++)
            {
                //ignore deleted matches
                if (trainIdx1[i, 0] == -1 && trainIdx1[i, 1] == -1)
                {
                    continue;
                }

                //for all matches image2 -> image1
                for (int j = 0; j < trainIdx2.Rows; j++)
                {
                    //ignore deleted matches
                    if (trainIdx2[j, 0] == -1 && trainIdx2[j, 1] == -1)
                    {
                        continue;
                    }

                    //Match symmetry test
                    //if (trainIdx1[i, 0] == trainIdx2[j, 1] &&
                    //    trainIdx1[i, 1] == trainIdx2[j, 0])
                    if (trainIdx1[i, 0] == j && trainIdx2[j, 0] == i)
                    {
                        symMatches[i, 0] = j;
                        symMatches[i, 1] = i;
                        symMatches[i, 2] = distance1[i, 0];
                        symMatches[i, 3] = distance1[i, 1];
                        count++;
                        break;
                    }
                }
            }
            return count;
        }


        /// <summary>
        /// Identify good matches using RANSAC
        /// </summary>/// symmetrical matches
        /// keypoint1
        /// keypoint2
        /// the number of symmetrical matches
        Matrix<double> ApplyRANSAC(Matrix<float> matches, VectorOfKeyPoint keyPoints1, VectorOfKeyPoint keyPoints2, int matchesNumber)
        {
            selPoints1 = new Matrix<float>(matchesNumber, 2);
            selPoints2 = new Matrix<float>(matchesNumber, 2);

            int selPointsIndex = 0;
            for (int i = 0; i < matches.Rows; i++)
            {
                if (matches[i, 0] == 0 && matches[i, 1] == 0)
                {
                    continue;
                }

                //Get the position of left keypoints
                float x = keyPoints1[(int)matches[i, 0]].Point.X;
                float y = keyPoints1[(int)matches[i, 0]].Point.Y;
                selPoints1[selPointsIndex, 0] = x;
                selPoints1[selPointsIndex, 1] = y;
                //Get the position of right keypoints
                x = keyPoints2[(int)matches[i, 1]].Point.X;
                y = keyPoints2[(int)matches[i, 1]].Point.Y;
                selPoints2[selPointsIndex, 0] = x;
                selPoints2[selPointsIndex, 1] = y;
                selPointsIndex++;
            }

            Matrix<double> fundamentalMatrix = new Matrix<double>(3, 3);
            //IntPtr status = CvInvoke.cvCreateMat(1, matchesNumber, MAT_DEPTH.CV_8U);
            //Matrix<double> status = new Matrix<double>(1, matchesNumber);
            IntPtr statusp = CvInvoke.cvCreateMat(1, matchesNumber, MAT_DEPTH.CV_8U);
            IntPtr points1 = CreatePointListPointer(selPoints1);
            IntPtr points2 = CreatePointListPointer(selPoints2);
            //IntPtr fundamentalMatrixp = CvInvoke.cvCreateMat(3, 3, MAT_DEPTH.CV_32F);

            //Compute F matrix from RANSAC matches
            CvInvoke.cvFindFundamentalMat(
                points1, //selPoints1   points in first image
                points2, //selPoints2   points in second image
                fundamentalMatrix,  //fundamental matrix 
                CV_FM.CV_FM_RANSAC, //RANSAC method
                this._Distance,  //Use 3.0 for default. The parameter is used for RANSAC method only.
                this._Confidence, //Use 0.99 for default. The parameter is used for RANSAC or LMedS methods only. 
                statusp);//The array is computed only in RANSAC and LMedS methods.

            Matrix<int> status = new Matrix<int>(1, matchesNumber, statusp);
            //Matrix<double> fundamentalMatrix = new Matrix<double>(3, 3, fundamentalMatrixp);
            if (this._RefineF)
            {
                matchesNumber = 0;
                for (int i = 0; i < status.Cols; i++)
                {
                    if (status[0, i] >= 1)  // ==1
                    {
                        matchesNumber++;
                    }
                }
                selPoints1 = new Matrix<float>(matchesNumber, 2);
                selPoints2 = new Matrix<float>(matchesNumber, 2);

                modelKeyPoints = new VectorOfKeyPoint();
                observedKeyPoints = new VectorOfKeyPoint();

                int statusIndex = -1;
                selPointsIndex = 0;
                for (int i = 0; i < matches.Rows; i++)
                {
                    if (matches[i, 0] == 0 && matches[i, 1] == 0)
                    {
                        continue;
                    }

                    statusIndex++;
                    if (status[0, statusIndex] >= 1)  // == 1
                    {
                        //Get the position of left keypoints
                        float x = keyPoints1[(int)matches[i, 0]].Point.X;
                        float y = keyPoints1[(int)matches[i, 0]].Point.Y;
                        selPoints1[selPointsIndex, 0] = x;
                        selPoints1[selPointsIndex, 1] = y;

                        MKeyPoint[] kpt = new MKeyPoint[1];
                        kpt[0] = new MKeyPoint();
                        kpt[0].Point.X = x; kpt[0].Point.Y = y;
                        modelKeyPoints.Push(kpt);

                        //Get the position of right keypoints
                        x = keyPoints2[(int)matches[i, 1]].Point.X;
                        y = keyPoints2[(int)matches[i, 1]].Point.Y;
                        selPoints2[selPointsIndex, 0] = x;
                        selPoints2[selPointsIndex, 1] = y;

                        MKeyPoint[] kpt2 = new MKeyPoint[1];
                        kpt2[0] = new MKeyPoint();
                        kpt2[0].Point.X = x; kpt2[0].Point.Y = y;
                        observedKeyPoints.Push(kpt2);
                        selPointsIndex++;
                    }
                }

                status = new Matrix<int>(1, matchesNumber);

                mask = new Matrix<byte>(matchesNumber, 1);
                for (int i = 0; i < mask.Rows; i++)
                {
                    mask[i, 0] = 0;  // don't draw lines, we will do it our selves
                }                    // set this to one if you wanted to use Features2DToolbox.DrawMatches
                // to draw correspondences

                indices = new Matrix<int>(matchesNumber, 2);   // not being used as we draw correspondences
                for (int i = 0; i < indices.Rows; i++)         // ourselves
                {
                    indices[i, 0] = i;  // has a problem in drawing lines, so we will drawe ourselves
                    indices[i, 1] = i;  // this is not being used in our code
                }

                //Compute F matrix from RANSAC matches   // we can do additional RANSAC filtering
                //CvInvoke.cvFindFundamentalMat(         // but first RANSAC gives good results so not used
                //    selPoints1, //points in first image
                //    selPoints2, //points in second image
                //    fundamentalMatrix,  //fundamental matrix 
                //    CV_FM.CV_FM_RANSAC, //RANSAC method
                //    this._Distance,  //Use 3.0 for default. The parameter is used for RANSAC method only.
                //    this._Confidence, //Use 0.99 for default. The parameter is used for RANSAC or LMedS methods only. 
                //    status);//The array is computed only in RANSAC and LMedS methods.
                // we will need to copy points from selPoints1 and 2 based on status if above was uncommented
            }
            return fundamentalMatrix;
        }

        //-----------converts a C# list to a C++ compatible IntPtr so that the openCV method can be called
        public IntPtr CreatePointListPointer(Matrix<float> points)
        {
            int _pointsCount = points.Rows;
            IntPtr result = CvInvoke.cvCreateMat(_pointsCount, 2, MAT_DEPTH.CV_32F);

            for (int i = 0; i < _pointsCount; i++)
            {
                double currentX = points[i, 0];
                double currentY = points[i, 1];
                CvInvoke.cvSet2D(result, i, 0, new MCvScalar(currentX));
                CvInvoke.cvSet2D(result, i, 1, new MCvScalar(currentY));
            }

            return result;
        }

        // draws matched correspondence points
        public static Image<Bgr, Byte> Draw(Image<Gray, Byte> modelImage, VectorOfKeyPoint modelKeyPoints,
            Image<Gray, byte> observedImage, VectorOfKeyPoint observedKeyPoints,
            Matrix<int> indices, Matrix<byte> mask)
        {
            Image<Bgr, Byte> resultImage = Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                    indices, new Bgr(255, 0, 0), new Bgr(0, 255, 0), mask, Features2DToolbox.KeypointDrawType.DEFAULT);

            return resultImage;
        }
    }
}
