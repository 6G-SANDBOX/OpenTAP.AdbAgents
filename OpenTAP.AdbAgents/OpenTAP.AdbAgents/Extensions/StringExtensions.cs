﻿// Author:      Bruno Garcia Garcia <bgg@uma.es>
// Copyright:   Copyright 2019-2020 Universidad de Málaga (University of Málaga), Spain

using System.Security;

namespace Tap.Plugins.UMA.Extensions
{
    public static class StringExtension
    {
        public static SecureString ToSecureString(this string value)
        {
            SecureString res = new SecureString();

            foreach (char c in value)
            {
                res.AppendChar(c);
            }

            return res;
        }
    }
}
