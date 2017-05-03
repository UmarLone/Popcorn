﻿using System.Collections.Generic;
using Popcorn.Models.Shows;

namespace Popcorn.Comparers
{
    public class ShowComparer : IEqualityComparer<ShowJson>
    {
        /// <summary>
        /// Compare two shows
        /// </summary>
        /// <param name="x">First show</param>
        /// <param name="y">Second show</param>
        /// <returns>True if both shows are the same, false otherwise</returns>
        public bool Equals(ShowJson x, ShowJson y)
        {
            //Check whether the compared objects reference the same data.
            if (ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;

            return x.ImdbId == y.ImdbId && x.Synopsis == y.Synopsis;
        }

        /// <summary>
        /// Define a unique hash code for a show
        /// </summary>
        /// <param name="show">A show</param>
        /// <returns>Unique hashcode</returns>
        public int GetHashCode(ShowJson show)
        {
            //Check whether the object is null
            if (ReferenceEquals(show, null)) return 0;

            //Get hash code for the Id field
            var hashId = show.ImdbId.GetHashCode();

            //Get hash code for the synopsis field.
            var hashSynopsis = show.Synopsis.GetHashCode();

            return hashId ^ hashSynopsis;
        }
    }
}
