namespace MiradorWorks.Settings
{
    /// <summary>Rules for settings ids, which are the persistence key./// </summary>
    public static class SettingId
    {
        public const int MaxLength = 64;

        /// <summary>
        /// Valid ids are 1-64 characters of ASCII letter, digits, '.', '_' or '-'.
        /// Example: "graphics.shadow".
        /// </summary>
        public static bool IsValid(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaxLength)
                return false;

            foreach (char c in id)
            {
                bool allowed =
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '.' || c == '_' || c == '-';

                if (!allowed)
                    return false;
            }

            return true;
        }
    }
}