﻿using System;
using System.IO;
using System.Drawing.Imaging;
using System.Linq;
using CommandLine;
using KangaModeling.Facade;

namespace KangaModeling.CommandLine
{

	/// <summary>
	/// Main class for running KangaModeling from cmd line.
	/// </summary>
	public sealed class CommandLine
	{

		public static int Main(String[] args)
		{
			Options opts = new Options();
			if (!new CommandLineParser().ParseArguments(args, opts))
			{
				Console.Error.WriteLine(opts.GetHelp());
				return -1;
			}
			new CommandLine().Run(opts);
			return 0;
		}

		internal void Run(Options opts)
		{
			string model = "";
			try
			{
				using (StreamReader sr = new StreamReader(opts.Model))
				{
					model = sr.ReadToEnd ();
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("The model file could not be read.");
				return;
			}

			var arguments = new DiagramArguments(model, DiagramType.Sequence, DiagramStyle.Classic);
			var result = DiagramFactory.Create(arguments);
            if(result.Errors.Count() != 0)
            {
                foreach (var de in result.Errors)
                    Console.Error.WriteLine(string.Format("{0}: {1}", de.Message, de.TokenValue));
                return;
            }

			using (result)
			{
				ImageFormat format;
				switch (opts.Format.ToLowerInvariant())
				{
					case "png":
						format = ImageFormat.Png;
						break;
					case "bmp":
						format = ImageFormat.Bmp;
						break;
					case "jpeg":
						format = ImageFormat.Jpeg;
						break;
					default:
						throw new ArgumentException("unknown format: " + opts.Format);
				}
				result.Image.Save(opts.FileName, format);
			}
		}
	}
}
