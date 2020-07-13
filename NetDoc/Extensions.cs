using System.Text;

namespace NetDoc
{
    public static class Extensions
    {
        public static string ToTitleCase(this string s)
        {
            var result=new StringBuilder();
            result.Append(char.ToUpper(s[0]));
            for (var i = 1; i < s.Length; i++)
            {
                if (s[i] == ' ' || s[i] == '.' || s[i] == '-')
                {
                    result.Append(char.ToUpper(s[++i]));
                }
                else
                {
                    result.Append(s[i]);
                }
            }

            return result.ToString();
        }
    }
}
