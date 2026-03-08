using System;
using UnityEngine;

namespace DataFerret.Analytics
{
    /// <summary>
    /// Manages user identity (userId and anonymousId) for event tracking.
    /// AnonymousId is persisted via PlayerPrefs across sessions.
    /// </summary>
    public class IdentityManager
    {
        private const string AnonymousIdKey = "df_anonymous_id";
        private string _userId;
        private string _anonymousId;

        /// <summary>Current user ID, or null if not identified.</summary>
        public string UserId => _userId;

        /// <summary>
        /// Anonymous ID, auto-generated on first access and persisted via PlayerPrefs.
        /// </summary>
        public string AnonymousId
        {
            get
            {
                if (string.IsNullOrEmpty(_anonymousId))
                {
                    _anonymousId = PlayerPrefs.GetString(AnonymousIdKey);
                    if (string.IsNullOrEmpty(_anonymousId))
                    {
                        _anonymousId = Guid.NewGuid().ToString();
                        PlayerPrefs.SetString(AnonymousIdKey, _anonymousId);
                        PlayerPrefs.Save();
                    }
                }
                return _anonymousId;
            }
        }

        /// <summary>Sets the user ID (called via Identify).</summary>
        /// <param name="userId">The user ID to associate with future events.</param>
        public void SetUserId(string userId)
        {
            _userId = userId;
        }

        /// <summary>
        /// Resets identity: clears userId and generates a new anonymousId.
        /// Used for logout scenarios.
        /// </summary>
        public void Reset()
        {
            _userId = null;
            _anonymousId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(AnonymousIdKey, _anonymousId);
            PlayerPrefs.Save();
        }
    }
}
