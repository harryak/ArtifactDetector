﻿/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using System;
using System.IO;

namespace VisualArtifactDetector.Helper
{
    class FileHelper
    {
        /// <summary>
        /// Helper function to ensure a path is ending with the right slash
        /// or backslash.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The path with a trailing directory separator.</returns>
        public static string AddDirectorySeparator(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            path = path.TrimEnd();

            if (PathEndsWithDirectorySeparator())
                return path;

            return path + GetDirectorySeparatorUsedInPath();

            bool PathEndsWithDirectorySeparator()
            {
                if (path.Length == 0)
                    return false;

                char lastChar = path[path.Length - 1];
                return lastChar == Path.DirectorySeparatorChar
                    || lastChar == Path.AltDirectorySeparatorChar;
            }

            char GetDirectorySeparatorUsedInPath()
            {
                if (path.Contains(Path.AltDirectorySeparatorChar.ToString()))
                    return Path.AltDirectorySeparatorChar;

                return Path.DirectorySeparatorChar;
            }
        }

        /// <summary>
        /// Writes to a file using the write action.
        /// </summary>
        /// <param name="filename">The filename to write to.</param>
        /// <param name="writeAction">An action writing to the stream.</param>
        /// <param name="fileMode"></param>
        public static void WriteToFile(string filename, Action<Stream> writeAction, FileMode fileMode = FileMode.OpenOrCreate)
        {
            Stream stream = null;
            try
            {
                stream = File.Open(filename, fileMode);
                writeAction(stream);
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }
    }
}