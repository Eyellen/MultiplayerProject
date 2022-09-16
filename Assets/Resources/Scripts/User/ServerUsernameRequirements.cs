namespace GameEngine.User
{
    public static class ServerUsernameRequirements
    {
        /// <summary>
        /// Takes username and otherNames as arguments and returns username if it's unique compared to otherNames, 
        /// otherwise returns username with corresponding postfix.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="otherNames">Other usernames to compare with</param>
        /// <returns>Unique username</returns>
        public static string GetUniqueUsername(string username, string[] otherNames)
        {
            int idx = 0;
            while (idx < otherNames.Length)
            {
                if (username == otherNames[idx])
                {
                    if (CheckIfHasPostfix(otherNames[idx], out int postfix))
                    {
                        int newPostfix = postfix + 1;
                        username = username.Replace(postfix.ToString(), newPostfix.ToString());
                    }
                    else
                    {
                        username += "_1";
                    }

                    idx = 0;
                    continue;
                }

                idx++;
            }

            return username;
        }

        /// <summary>
        /// Finds postfix in username and passes it to out postfix argument.
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <param name="postfix">Postfix from username. If username has no postfix equals 0</param>
        /// <returns>True if username has postfix, False otherwise</returns>
        private static bool CheckIfHasPostfix(string username, out int postfix)
        {
            int idx = username.Length - 1;
            while (idx > 0)
            {
                if (username[idx - 1] == '_' && (username[idx] >= '0' && username[idx] <= '9')) break;

                if (idx - 1 <= 0)
                {
                    postfix = default(int);
                    return false;
                }

                idx--;
            }

            postfix = int.Parse(username.Substring(idx, username.Length - idx));
            return true;
        }
    }
}
