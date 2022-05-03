﻿using ArknightsResources.Operators.Models;
using AssetStudio;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ArknightsResources.Operators.Resources
{
    internal static class AssetBundleHelper
    {
        // 本类使用AssetStudio项目来读取AssetBundle文件
        // AssetStudio项目地址:https://github.com/Perfare/AssetStudio
        // 下面附上AssetStudio项目的许可证原文
        #region LICENSE
        /*
        MIT License

        Copyright (c) 2016 Radu
        Copyright (c) 2016-2020 Perfare

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
         */
        #endregion

        public static byte[] GetOperatorIllustration(byte[] assetBundleFile, string fileName, OperatorIllustrationInfo illustrationInfo)
        {
            Image<Bgra32> rgb = null;
            Image<Bgra32> alpha = null;
            HandleAbPacksInternal(assetBundleFile, fileName, illustrationInfo, ref rgb, ref alpha);
            return ImageHelper.ProcessImage(rgb, alpha);
        }

        public static Image<Bgra32> GetOperatorIllustrationReturnImage(byte[] assetBundleFile, string fileName, OperatorIllustrationInfo illustrationInfo)
        {
            Image<Bgra32> rgb = null;
            Image<Bgra32> alpha = null;
            HandleAbPacksInternal(assetBundleFile, fileName, illustrationInfo, ref rgb, ref alpha);
            return ImageHelper.ProcessImageReturnImage(rgb, alpha);
        }

        private static void HandleAbPacksInternal(byte[] assetBundleFile, string fileName, OperatorIllustrationInfo illustrationInfo, ref Image<Bgra32> rgb, ref Image<Bgra32> alpha)
        {
            using (MemoryStream stream = new MemoryStream(assetBundleFile))
            {
                AssetsManager assetsManager = new AssetsManager();
                string path = Path.GetFullPath(fileName);
                FileReader reader = new FileReader(path, stream);
                assetsManager.LoadFile(path, reader);
                IEnumerable<Texture2D> targets = from asset
                                                 in assetsManager.assetsFileList.FirstOrDefault().Objects
                                                 where IsTexture2DMatch(asset, illustrationInfo)
                                                 select (asset as Texture2D);

                foreach (var item in targets)
                {
                    if (item.m_Name.Contains("[alpha]"))
                    {
                        alpha = item.ConvertToImage();
                    }
                    else
                    {
                        rgb = item.ConvertToImage();
                    }
                }
            }
        }

        private static bool IsTexture2DMatch(AssetStudio.Object asset, OperatorIllustrationInfo info)
        {
            if (asset.type == ClassIDType.Texture2D)
            {
                Texture2D texture2D = (Texture2D)asset;
                if (texture2D.m_Width <= 512 || texture2D.m_Height <= 512)
                {
                    return false;
                }

                Match match;
                if (info.Type == OperatorType.Skin)
                {
                    match = Regex.Match(texture2D.m_Name, $@"char_[\d]*_({info.ImageCodename})#([\d]*)(b?)(\[alpha\])?",
                                          RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[3].Value))
                    {
                        return false;
                    }
                }
                else
                {
                    match = Regex.Match(texture2D.m_Name, $@"char_[\d]*_({info.ImageCodename}\+?)(?!b)(\[alpha\])?");
                }

                return match.Success && string.Equals(match.Groups[1].Value, info.ImageCodename);
            }
            else
            {
                return false;
            }
        }

        private static Image<Bgra32> ConvertToImage(this Texture2D m_Texture2D)
        {
            ResourceReader reader = m_Texture2D.image_data;
            byte[] originData = InternalArrayPools.ByteArrayPool.Rent(reader.Size);
            reader.GetData(originData);

            byte[] data = ImageHelper.DecodeETC1(originData, m_Texture2D.m_Width, m_Texture2D.m_Height);

            try
            {
                var image = Image.LoadPixelData<Bgra32>(data, m_Texture2D.m_Width, m_Texture2D.m_Height);
                image.Mutate(x => x.Flip(FlipMode.Vertical));
                return image;
            }
            finally
            {
                InternalArrayPools.ByteArrayPool.Return(originData);
                InternalArrayPools.ByteArrayPool.Return(data);
            }
        }
    }
}
