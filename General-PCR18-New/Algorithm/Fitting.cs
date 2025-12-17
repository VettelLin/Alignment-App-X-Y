using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public class Fitting
    {
        double[] m_pdXlist;
        double[] m_pdYlist;
        int m_iListLen;
        int m_iMjie;
        double m_dXListAver, m_dYListAver; //x列和y列的平均值
        double[] m_pCMatrix; //系数矩阵
        double[] m_pAMatrix; //C的转置乘C+C的转置乘Y，N行，M+1列
        double[] m_pdFinalCoef;

        /// <summary>
        /// 生成矛盾方程的系数矩阵m_pCMatrix
        /// </summary>
        /// <returns></returns>
        public bool FormCLEG()
        {
            int i, j;
            double x, temp;

            if (m_pCMatrix != null)
            {
                m_pCMatrix = null;
            }
            m_pCMatrix = new double[m_iListLen * m_iMjie];
            if (null == m_pCMatrix)
            {
                return false;
            }
            for (i = 0; i < m_iListLen; i++)
            {
                x = m_pdXlist[i];
                temp = 1;
                for (j = 0; j < m_iMjie; j++)
                {
                    m_pCMatrix[i * m_iMjie + j] = temp;
                    temp *= x;
                }
            }

            return true;
        }

        /// <summary>
        /// 生成正规方程的增广矩阵m_pAMatrix
        /// </summary>
        /// <returns></returns>
        public bool FormNormalEquation()
        {
            int i, j, k;
            double temp;

            if (m_pAMatrix != null)
            {
                m_pAMatrix = null;
            }
            m_pAMatrix = new double[m_iMjie * (m_iMjie + 1)]; //为增广矩阵分配空间 M*(M+1)
            if (null == m_pAMatrix)
            {
                return false;
            }
            ////////////////////////////////////
            for (i = 0; i < m_iMjie; i++)
                for (j = i; j < m_iMjie; j++) //C的转置乘C
                {
                    temp = 0;
                    for (k = 0; k < m_iListLen; k++)
                    {
                        temp += m_pCMatrix[k * m_iMjie + i] * m_pCMatrix[k * m_iMjie + j];
                    }
                    m_pAMatrix[i * (m_iMjie + 1) + j] = temp;
                    m_pAMatrix[j * (m_iMjie + 1) + i] = temp;
                }
            for (i = 0; i < m_iMjie; i++) //C的转置乘Y
            {
                temp = 0;
                for (k = 0; k < m_iListLen; k++)
                {
                    temp += m_pCMatrix[k * m_iMjie + i] * m_pdYlist[k];
                }
                m_pAMatrix[i * (m_iMjie + 1) + m_iMjie] = temp;
            }
            return true;
        }

        /// <summary>
        /// 消元，求线性方程组的解
        /// </summary>
        /// <returns></returns>
        public int Gauss()
        {
            int i, j, k;
            double x = 0;
            int MatrixA_ArrNum = m_iMjie + 1;
            for (k = 0; k < m_iMjie; k++)
            {
                x = m_pAMatrix[k * MatrixA_ArrNum + k];
                for (j = k; j < MatrixA_ArrNum; j++)
                {
                    m_pAMatrix[k * MatrixA_ArrNum + j] /= x;
                }
                if (k == m_iMjie - 1)
                {
                    break;
                }
                for (i = k + 1; i < m_iMjie; i++)
                {
                    x = m_pAMatrix[i * MatrixA_ArrNum + k];
                    for (j = k; j < MatrixA_ArrNum; j++)
                    {
                        m_pAMatrix[i * MatrixA_ArrNum + j] -= x * m_pAMatrix[k * MatrixA_ArrNum + j];
                    }
                }

            }
            for (k = m_iMjie - 1; k > 0; k--)
            {
                for (i = k - 1; i >= 0; i--)
                {
                    m_pAMatrix[i * MatrixA_ArrNum + m_iMjie] -= m_pAMatrix[i * MatrixA_ArrNum + k] * m_pAMatrix[k * MatrixA_ArrNum + m_iMjie];
                    m_pAMatrix[i * MatrixA_ArrNum + k] = 0;
                }
            }
            return 0;
        }

        /// <summary>
        /// 求相关系数R^2
        /// </summary>
        /// <returns></returns>
        public double RelatedCoef()
        {
            int l, m;
            double SD = 0, temp = 0, Var = 0, dRelatedCoef = 0;
            for (l = 0; l < m_iListLen; l++) //求标准差SD
            {
                temp = 0;
                for (m = 0; m < m_iMjie; m++)
                {
                    temp += m_pdFinalCoef[m] * m_pCMatrix[l * m_iMjie + m];
                }
                temp -= m_pdYlist[l];
                SD += temp * temp;
            }

            temp = 0;
            for (l = 0; l < m_iListLen; l++) //求Var＝Sum(Yi-Aver(Yi))^2
            {
                temp += Math.Pow((m_pdYlist[l] - m_dYListAver), (double)2);
            }
            Var = temp;
            if (Var == 0)
            {
                Var = 0.000000001;
            }
            dRelatedCoef = 1 - SD / Var;
            return dRelatedCoef;
        }

        /// <summary>
        /// 求解
        /// </summary>
        /// <param name="x">X列的值</param>
        /// <param name="y">Y列的值</param>
        /// <param name="dCoef">外部调用的多项式拟和的系数数组</param>
        /// <param name="len">X列和Y列的长度</param>
        /// <param name="m">多项式拟和的阶数</param>
        /// <returns>各点误差的平方和</returns>
        public double SolutionCLEG(double[] x, double[] y, double[] dCoef, int len, int m) {
            int k, ErrCode = 0;
            double dbRelatedCoef = 0;
            double SumX = 0, SumY = 0;

            m_iListLen = len;
            m_iMjie = m;

            if (m_pdXlist != null)
            {
                m_pdXlist = null;
            }
            m_pdXlist = new double[m_iListLen];
            if (null == m_pdXlist)
            {
                return 0;
            }

            if (m_pdYlist != null)
            {
                m_pdYlist = null;
            }
            m_pdYlist = new double[m_iListLen];
            if (null == m_pdYlist)
            {
                return 0;
            }

            if (m_pdFinalCoef != null)
            {
                m_pdFinalCoef = null;
            }
            m_pdFinalCoef = new double[m_iMjie];
            if (null == m_pdFinalCoef)
            {
                return 0;
            }

            for (k = 0; k < m_iListLen; k++)
            {
                m_pdXlist[k] = x[k];
                SumX = SumX + x[k];
                m_pdYlist[k] = y[k];
                SumY = SumY + y[k];
            }
            m_dXListAver = SumX / m_iListLen;
            m_dYListAver = SumY / m_iListLen;

            FormCLEG();
            FormNormalEquation();
            ErrCode = Gauss();
            if (ErrCode > 0)
            {
                return -2;
            }

            for (k = 0; k < m_iMjie; k++)
            {
                m_pdFinalCoef[k] = m_pAMatrix[k * (m_iMjie + 1) + m_iMjie];
            }

            dbRelatedCoef = RelatedCoef();

            Array.Copy(m_pdFinalCoef, dCoef, m_iMjie);

            return dbRelatedCoef;
        }

    }
}
