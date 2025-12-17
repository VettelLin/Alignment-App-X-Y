using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public enum SortType { BUBBLE = 0, INSERTION, QUICK, SELECTION }
    public enum DataType { MYLONG = 0, MYDOUBLE, MYINTEGER }

    public class PcrAnalysisSort<T> where T : IComparable<T>
    {
        private delegate void SortMethod(T[] array);
        private readonly List<SortMethod> sortMethods;
        private int size;

        public PcrAnalysisSort(int size)
        {
            if (size < 0) return;
            this.size = size;

            sortMethods = new List<SortMethod>
            {
                BubbleSort,
                InsertionSort,
                QuickSortHT,
                SelectionSort
            };
        }

        public bool SelectSortType(int sortType, T[] array)
        {
            if (sortType < (int)SortType.BUBBLE || sortType > (int)SortType.SELECTION)
                return false;

            sortMethods[sortType](array);
            return true;
        }

        private void BubbleSort(T[] array)
        {
            T temp;
            int last = size - 1;
            bool sorted;

            do
            {
                sorted = true;
                for (int i = 0; i < last; i++)
                {
                    if (array[i].CompareTo(array[i + 1]) > 0)
                    {
                        temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                        sorted = false;
                    }
                }
                last--;
            } while (!sorted);
        }

        private void InsertionSort(T[] array)
        {
            T cVal;

            for (int i = 1; i < size; i++)
            {
                cVal = array[i];
                int n = i - 1;

                while (n >= 0 && cVal.CompareTo(array[n]) < 0)
                {
                    array[n + 1] = array[n];
                    n--;
                }

                array[n + 1] = cVal;
            }
        }

        private void QuickSort(T[] array, int llimit, int rlimit)
        {
            int left = llimit;
            int right = rlimit;
            int pivot = (left + right) / 2;
            T median = array[pivot];
            T temp;

            do
            {
                while (array[left].CompareTo(median) < 0 && left < rlimit) left++;
                while (median.CompareTo(array[right]) < 0 && right > llimit) right--;

                if (left <= right)
                {
                    temp = array[left];
                    array[left] = array[right];
                    array[right] = temp;
                    left++;
                    right--;
                }
            } while (left <= right);

            if (llimit < right) QuickSort(array, llimit, right);
            if (left < rlimit) QuickSort(array, left, rlimit);
        }

        private void QuickSortHT(T[] array)
        {
            QuickSort(array, 0, size - 1);
        }

        private void SelectionSort(T[] array)
        {
            T temp;
            int min;

            for (int i = 0; i < size - 1; i++)
            {
                min = i;

                for (int n = i + 1; n < size; n++)
                {
                    if (array[n].CompareTo(array[min]) < 0)
                    {
                        min = n;
                    }
                }

                temp = array[min];
                array[min] = array[i];
                array[i] = temp;
            }
        }
    }
}
