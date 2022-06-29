/* CIL Tools
 * Copyright (c) 2022,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;

namespace CilTools.Tests.Common
{
    public enum DiffKind
    {
        Unknown = 0, Addition = 1, Deletion, Same
    }

    public class DiffElement
    {
        public DiffElement(DiffKind kind, string val)
        {
            this.Kind = kind;
            this.Value = val;
        }

        public DiffKind Kind { get; set; }
        public string Value { get; set; }
    }

    public class StringDiff
    {
        List<DiffElement> items = new List<DiffElement>();

        IEnumerable<DiffElement> Items
        {
            get { return items.ToArray(); }
        }

        static bool CollectionContainsSubsequence<T>(IList<T> coll, IList<T> subsequence)
        {
            if (coll.Count == 0 || subsequence.Count == 0)
            {
                return false;
            }

            if (coll.Count < subsequence.Count)
            {
                return false;
            }

            int prefixCountEqual = 0;

            for (int i = 0; i < subsequence.Count; i++)
            {
                if (!coll.Contains(subsequence[i])) return false;

                if (coll[i].Equals(subsequence[i])) prefixCountEqual++;
            }

            // Subsequence is prefix of coll
            if (prefixCountEqual == subsequence.Count) return true;

            int coll_counter = 0;
            int subsequence_counter = 0;

            int count_equal = 0;

            while (true)
            {
                if (coll_counter >= coll.Count)
                {
                    break;
                }

                if (subsequence_counter >= subsequence.Count)
                {
                    break;
                }

                if (coll[coll_counter].Equals(subsequence[subsequence_counter]))
                {
                    coll_counter++;
                    subsequence_counter++;
                    count_equal++;

                    if (count_equal == subsequence.Count)
                    {
                        return true;
                    }

                    continue;
                }
                else
                {
                    coll_counter++;
                }
            }

            return count_equal == subsequence.Count;
        }

        static IList<T> GetSubCollection<T>(IList<T> coll, int index, int length)
        {
            if (index < 0)
            {
                index = 0;
            }

            List<T> ret = new List<T>(coll.Count);

            for (int i = index; i < index + length; i++)
            {
                if (i >= coll.Count)
                {
                    break;
                }

                ret.Add(coll[i]);
            }

            return ret;
        }

        static IList<T> GetCommonPrefix<T>(IList<T> left, IList<T> right)
        {
            List<T> ret = new List<T>(left.Count);

            for (int i = 0; i < left.Count; i++)
            {
                if (i >= right.Count) break;

                if (left[i].Equals(right[i])) ret.Add(left[i]);
                else break;
            }

            return ret;
        }

        static IList<T> GetCollectionsLongestCommonSubsequence<T>(IList<T> left, IList<T> right)
        {
            if (left.Count == 0 || right.Count == 0)
            {
                return new T[0];
            }

            IList<T> prefix = GetCommonPrefix(left, right);

            if (prefix.Count > 0)
            {
                //have common prefix
                IList<T> new_left = GetSubCollection(left, prefix.Count, left.Count - prefix.Count);
                IList<T> new_right = GetSubCollection(right, prefix.Count, right.Count - prefix.Count);
                IList<T> inner_subs = GetCollectionsLongestCommonSubsequence(new_left, new_right);

                List<T> list = new List<T>(prefix);
                for (int i = 0; i < inner_subs.Count; i++) list.Add(inner_subs[i]);
                return list;
            }

            if (!right.Contains(left[0]))
            {
                IList<T> new_left = GetSubCollection(left, 1, left.Count - 1);
                return GetCollectionsLongestCommonSubsequence(new_left, right);
            }

            if (!left.Contains(right[0]))
            {
                IList<T> new_right = GetSubCollection(right, 1, right.Count - 1);
                return GetCollectionsLongestCommonSubsequence(left, new_right);
            }

            if (CollectionContainsSubsequence(right, left))
            {
                return left;
            }

            if (CollectionContainsSubsequence(left, right))
            {
                return right;
            }

            if (left.Count == 1 || right.Count == 1)
            {
                return new T[0];
            }

            IList<T> ret = new T[0];
            T x = left[left.Count - 1];
            T y = right[right.Count - 1];

            if (x.Equals(y))
            {
                List<T> list = new List<T>(GetCollectionsLongestCommonSubsequence(
                    GetSubCollection(left, 0, left.Count - 1),
                    GetSubCollection(right, 0, right.Count - 1)));
                list.Add(x);
                return list;
            }
            else
            {
                IList<T> lcs1 = GetCollectionsLongestCommonSubsequence(
                    left,
                    GetSubCollection(right, 0, right.Count - 1));

                if (lcs1.Count > right.Count)
                {
                    return lcs1;
                }

                IList<T> lcs2 = GetCollectionsLongestCommonSubsequence(
                    GetSubCollection(left, 0, left.Count - 1),
                    right);

                if (lcs1.Count > lcs2.Count)
                {
                    return lcs1;
                }
                else
                {
                    return lcs2;
                }
            }
        }

        static string[] BreakString(string src, int maxlen)
        {
            List<string> ret = new List<string>();

            int i = 0;
            int len;

            while (true)
            {
                if (i >= src.Length)
                {
                    break;
                }

                if (i + maxlen > src.Length)
                {
                    len = src.Length - i;
                }
                else
                {
                    len = maxlen;
                }

                string s = src.Substring(i, len);
                ret.Add(s);
                i += len;
            }

            return ret.ToArray();
        }

        static string GetLongestCommonSubsequenceImpl(string left, string right)
        {
            IList<char> lcs = GetCollectionsLongestCommonSubsequence(left.ToCharArray(), right.ToCharArray());
            char[] chars = new char[lcs.Count];

            for (int i = 0; i < lcs.Count; i++) chars[i] = lcs[i];

            return new string(chars);
        }

        public static string GetLongestCommonSubsequence(string left, string right)
        {
            StringBuilder sb;

            if (left.Contains("\n") || right.Contains("\n"))
            {
                //for multiline documents, diff by lines
                string[] lines1 = left.Split(new char[] { '\r', '\n' });
                string[] lines2 = right.Split(new char[] { '\r', '\n' });

                int nLines = Math.Min(lines1.Length, lines2.Length);

                if (nLines > 2)
                {
                    sb = new StringBuilder(left.Length);

                    for (int i = 0; i < nLines; i++)
                    {
                        string s = GetLongestCommonSubsequence(lines1[i], lines2[i]);
                        sb.Append(s);
                        if (s.Length > 0) sb.AppendLine();
                    }

                    return sb.ToString();
                }
            }

            if (left.Length < 10 && right.Length < 10)
            {
                //short strings - we can calc direct diff
                return GetLongestCommonSubsequenceImpl(left, right);
            }

            if (left.Length > 80 || right.Length > 80)
            {
                //for larger strings, break into segments and diff them
                string[] arr1 = BreakString(left, 40);
                string[] arr2 = BreakString(right, 40);
                int n = Math.Min(arr1.Length, arr2.Length);
                sb = new StringBuilder(left.Length);

                for (int i = 0; i < n; i++)
                {
                    string s = GetLongestCommonSubsequence(arr1[i], arr2[i]);
                    sb.Append(s);
                }

                return sb.ToString();
            }

            string[] strings1 = left.Split(new char[] { ' ' });
            string[] strings2 = right.Split(new char[] { ' ' });

            if (strings1.Length <= 4 || strings2.Length <= 4)
            {
                //we don't have efficient solution for now, so just ignore this case
                return string.Empty;
            }

            //for other cases, diff by words
            IList<string> lcs = GetCollectionsLongestCommonSubsequence(strings1, strings2);

            sb = new StringBuilder(left.Length);

            for (int i = 0; i < lcs.Count; i++)
            {
                sb.Append(lcs[i]);
                if (i < lcs.Count - 1) sb.Append(' ');
            }

            return sb.ToString();
        }

        static StringDiff GetDeletions(string original, string lcs)
        {
            int iOriginal = 0;
            int iLCS = 0;
            StringDiff ret = new StringDiff();

            while (true)
            {
                if (iOriginal >= original.Length) break;
                if (iLCS >= lcs.Length) break;

                char c1 = original[iOriginal];
                char c = lcs[iLCS];

                if (c == c1)
                {
                    ret.items.Add(new DiffElement(DiffKind.Same, c.ToString()));
                    iOriginal++;
                    iLCS++;
                }
                else
                {
                    ret.items.Add(new DiffElement(DiffKind.Deletion, c1.ToString()));
                    iOriginal++;
                }
            }
            ;

            if (iOriginal < original.Length - 1)
            {
                string remainder = original.Substring(iOriginal);
                ret.items.Add(new DiffElement(DiffKind.Deletion, remainder));
            }

            return ret;
        }

        public static StringDiff GetDiff(string original, string changed)
        {
            string lcs = GetLongestCommonSubsequence(original, changed);
            return GetDeletions(original, lcs);
        }
    }
}
