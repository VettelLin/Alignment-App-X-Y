using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public class LnFitting
    {
        Fitting cFitting = new Fitting();

        double[] m_pdXList;
        double[] m_pdYList;
        double[] m_pdCoeffExpon;
        int m_iListLength;

        /// <summary>
        /// 对x列取ln
        /// </summary>
        /// <returns></returns>
        public bool LnX()
        {
            int i;
            for (i = 0; i < m_iListLength; i++)
            {
                m_pdXList[i] = Math.Log(m_pdXList[i]);
            }
            return true;
        }

        /// <summary>
        /// 求对数拟和：y=klnx+b 的系数k和常数项b
        /// </summary>
        /// <param name="x">x列数据，double型</param>
        /// <param name="y">y列数据，double型</param>
        /// <param name="Result">外部获得的计算结果，Result[0]为拟和得到的常数项b，Result[1]为系数k。</param>
        /// <param name="iListLen">x列和y列的长度</param>
        /// <returns>各点误差的平方和</returns>
        public double LnSolution(double[] x, double[] y, double[] Result, int iListLen)
        {
            double fSD;
            int k;
            m_iListLength = iListLen;
            m_pdXList = new double[m_iListLength];
            m_pdYList = new double[m_iListLength];
            m_pdCoeffExpon = new double[2];

            for (k = 0; k < m_iListLength; k++)
            {
                m_pdXList[k] = x[k];
                m_pdYList[k] = y[k];
            }
            LnX();
            fSD = cFitting.SolutionCLEG(m_pdXList, m_pdYList, m_pdCoeffExpon, m_iListLength, 2);
        
            Array.Copy(m_pdCoeffExpon, Result, 16);

            return fSD;
        }
    }
}
