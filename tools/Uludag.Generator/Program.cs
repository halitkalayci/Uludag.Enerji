using System.Text;
using Uludag.Generator;

// Türkçe çıktı her terminalde doğru görünsün diye konsol UTF-8 yazar (Windows varsayılan kod sayfası yerine).
Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

// Giriş noktası: çıktı stdout/stderr'e yazılır, çıkış kodu App'ten gelir.
return App.Run(args, Directory.GetCurrentDirectory(), Console.Out, Console.Error);
