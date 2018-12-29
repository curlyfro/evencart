﻿using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using RoastedMarketplace.Core.Infrastructure.Providers;
using RoastedMarketplace.Data.Entity.MediaEntities;
using RoastedMarketplace.Data.Helpers;

namespace RoastedMarketplace.Infrastructure.MediaServices
{
    public class MediaAccountant : IMediaAccountant
    {
        private const string PictureFileNameFormat = "{0}_{1}_{2}{3}"; //name_width_height.extension

        private readonly ILocalFileProvider _localFileProvider;
        private readonly IImageProcessor _imageProcessor;
        public MediaAccountant(ILocalFileProvider localFileProvider, IImageProcessor imageProcessor)
        {
            _localFileProvider = localFileProvider;
            _imageProcessor = imageProcessor;
        }

        public Media GetMediaInstance(IFormFile mediaFile, int userId)
        {
            byte[] fileBytes;
            using (var stream = new MemoryStream())
            {
                mediaFile.CopyToAsync(stream);
                fileBytes = stream.ToArray();
            }

            var saveDirectory = ApplicationEngine.MapPath(ApplicationConfig.MediaDirectory, true);
            var fileName = SafeWriteBytesToFile(mediaFile.FileName, saveDirectory, fileBytes);
            
            //file extension and it's type
            var fileExtension = _localFileProvider.GetExtension(mediaFile.FileName);
            if (!string.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            var contentType = mediaFile.ContentType;
            //an image?
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = PictureUtility.GetContentType(fileExtension);
            }
            //no? a video?
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = VideoUtility.GetContentType(fileExtension);
            }
            //no? let it be default one then
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/octet-stream";
            }
            var media = new Media() {
                MimeType = contentType,
                UserId = userId,
                Name = mediaFile.FileName,
                CreatedOn = DateTime.UtcNow,
                SystemName = fileName,
                AlternativeText = mediaFile.FileName
            };
            media.ThumbnailPath = GetPictureUrl(media, 150, 150, true);
            media.LocalPath = GetPictureUrl(media, 0, 0, true);
            return media;
        }

        public string GetPictureUrl(Media picture, int width = 0, int height = 0, bool returnDefaultIfNotFound = false)
        {
            if(picture == null)
                return returnDefaultIfNotFound ? ApplicationEngine.MapPath(ApplicationConfig.DefaultImagePath, true) : null;

            var serveFileDirectory = ApplicationEngine.MapPath(ApplicationConfig.MediaServeDirectory, true);

            //abc
            var fileNameWithoutExtension = _localFileProvider.GetFileNameWithoutExtension(picture.SystemName);
            //.jpg
            var extension = _localFileProvider.GetExtension(picture.SystemName);
            //abc_150_150.jpg
            var fileName = string.Format(PictureFileNameFormat, fileNameWithoutExtension, width, height, extension);
            //c:\\www\\abc_150_150.jpg
            var fileNameWithDirectory = _localFileProvider.CombinePaths(serveFileDirectory, fileName);

            if (_localFileProvider.FileExists(fileNameWithDirectory))
                return ApplicationEngine.MapUrl(fileNameWithDirectory); // /abc_150_150.jpg

            //we'll need to create a file
            //first resize the image from actual file
            var mediaFileDirectory = ApplicationEngine.MapPath(ApplicationConfig.MediaDirectory, true);
            var originalFile = _localFileProvider.CombinePaths(mediaFileDirectory, picture.SystemName);
            //does the original file exist?
            if(!_localFileProvider.FileExists(originalFile))
                return returnDefaultIfNotFound ? ApplicationEngine.MapPath(ApplicationConfig.DefaultImagePath, true) : null;
            //read original file
            var originalFileBytes = _localFileProvider.ReadBytes(originalFile);
            //resize the image
            var resizedBytes = _imageProcessor.ResizeImage(originalFileBytes, width, height);
            //write this to disk
            var resizedFileName = SafeWriteBytesToFile(fileName, serveFileDirectory, resizedBytes);
            fileNameWithDirectory = _localFileProvider.CombinePaths(serveFileDirectory, resizedFileName);
            //now return
            return ApplicationEngine.MapUrl(fileNameWithDirectory); // /abc_150_150.jpg
        }

        public string GetPictureUrl(Media picture, string size, bool returnDefaultIfNotFound = false)
        {
            //parse size
            var parsedSize = PictureUtility.GetSize(size);
            return GetPictureUrl(picture, parsedSize.Width, parsedSize.Height, returnDefaultIfNotFound);
        }

        public string GetVideoUrl(Media media)
        {
            throw new System.NotImplementedException();
        }


        private string SafeWriteBytesToFile(string fileName, string saveDirectory, byte[] bytes)
        {
            //write file to disk
            _localFileProvider.GetSafeFileName(fileName, saveDirectory, out fileName, out string fileNameWithPath);
            _localFileProvider.WriteBytes(fileNameWithPath, bytes);
            return fileName;
        }

    }
}