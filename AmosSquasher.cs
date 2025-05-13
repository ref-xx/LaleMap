using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;

namespace LaleMapTest
{
    //amos picture compactor c# version by ref
    // WARNING --- THIS IS NOT WORKING YET!
    // squash ya da planar üretme fonksiyonunda bir hata mevcut.
    // (planar fonksiyonu deluxePaint için çalışıyor, Citacop'tan alındı)
    
    internal class AmosSquasher
    {
        // AMOS sıkıştırma başlığı sihirli sayısı
        private const uint BMCode = 0x06071963; // MakePalette fonksiyonunuzda aranan değer

        /// <summary>
        /// AMOS 68k sıkıştırma algoritmasına göre bitplane verisini sıkıştırır.
        /// </summary>
        /// <param name="uncompressedPlanes">Her bir elemanı bir bitplane'in byte verisi olan byte dizileri dizisi.</param>
        /// <param name="tsize">Sıkıştırma blok yüksekliği (pixel satırı sayısı). 68k'daki Pktcar.</param>
        /// <param name="heightLines">Resmin toplam yüksekliği (pixel satırı sayısı). 68k'daki Pkdy.</param>
        /// <param name="widthBytes">Her bir bitplane satırının byte cinsinden genişliği (byte/scanline). 68k'daki Pkdx ve Pktx.</param>
        /// <returns>Sıkıştırılmış veriyi içeren byte dizisi.</returns>
        public static byte[] Squash(byte[][] uncompressedPlanes, int tsize, int heightLines, int widthBytes)
        {
            // 1. Giriş Doğrulamaları
            if (uncompressedPlanes == null || uncompressedPlanes.Length == 0)
            {
                return new byte[0]; // Boş giriş
            }
            int numPlanes = uncompressedPlanes.Length;
            if (heightLines <= 0 || widthBytes <= 0 || tsize <= 0)
            {
                throw new ArgumentException("Height, width, and tsize must be positive.");
            }
            if (heightLines % tsize != 0)
            {
                throw new ArgumentException($"Height ({heightLines}) must be divisible by tsize ({tsize}).");
            }

            // Bitplane boyutlarını kontrol et
            int expectedPlaneSize = heightLines * widthBytes;
            for (int i = 0; i < numPlanes; i++)
            {
                if (uncompressedPlanes[i] == null || uncompressedPlanes[i].Length != expectedPlaneSize)
                {
                    throw new ArgumentException($"Bitplane {i} has incorrect size. Expected {expectedPlaneSize}, got {uncompressedPlanes[i]?.Length ?? 0}.");
                }
            }

            // 2. Başlık Boyutlarını Hesapla (68k Değişkenleri)
            int Pkdx = widthBytes;
            int Pkdy = heightLines;
            int Pktcar = tsize;
            int Pknplan = numPlanes;
            int Pktx = widthBytes; // Genişlik blok sayısı (her blok 1 byte genişliğinde)
            int Pkty = heightLines / tsize; // Yükseklik blok sayısı (her blok tsize satır yüksekliğinde)

            // 3. Ara Kontrol Buffer'ı Boyutunu Hesapla (68k'daki 'a6' buffer'ı)
            // Bu buffer, orijinal verideki her bir byte için 1 bit içerir (değişip değişmediğini belirtmek için).
            int totalBytesToProcess = Pknplan * Pkty * Pktx * Pktcar; // Bu aslında numPlanes * heightLines * widthBytes
            int controlBufferSize = (totalBytesToProcess + 7) / 8 + 2; // +2 padding 68k kodu ile eşleşmesi için

            // 4. Gerekli Buffer'ları Hazırla
            byte[] controlBuffer = new byte[controlBufferSize]; // RLE kontrol bitlerini geçici olarak tutar, varsayılan 0
            List<byte> mainData = new List<byte>();             // Ana sıkıştırılmış veriyi toplar (68k'daki a5)
            byte[] prevBytes = new byte[Pknplan];               // Her bitplane için önceki byte değerini tutar (68k'daki d0 benzeri)

            // 5. Ana Sıkıştırma Döngüsü (68k plan -> ligne -> carre -> oct0 sırasını tersine simüle et)
            // 68k kodu adresleri sondan başa doğru işler (subq, dbra). C# kodu okumayı kolaylaştırmak için 0'dan başlayarak
            // indeksleri hesaplar, ancak döngü sırasını (plane, block row, block col, vertical offset) 68k ile aynı (ters) tutar.
            int controlBufferIndex = 0; // controlBuffer içinde güncel byte indeksi
            int controlBit = 7;         // controlBuffer[controlBufferIndex] içinde güncel bit konumu (7 en yüksek bit)

            for (int p = Pknplan - 1; p >= 0; p--) // plane döngüsü (örneğin 5'ten 0'a)
            {
                prevBytes[p] = 0; // Her yeni bitplane için önceki byte'ı sıfırla (68k d0'ı sıfırlamak gibi)

                for (int br = Pkty - 1; br >= 0; br--) // block row döngüsü (örneğin Pkty-1'den 0'a)
                {
                    for (int bc = Pktx - 1; bc >= 0; bc--) // block column döngüsü (örneğin Pktx-1'den 0'a)
                    {
                        // oct0 döngüsü: blok içindeki dikey byte'lar (tsize kadar)
                        // 68k'da bu döngü EcTLigne (widthBytes) ekleyerek aşağı doğru ilerler.
                        // C#'ta düz indis hesaplama ile yapılır.
                        for (int vo = Pktcar - 1; vo >= 0; vo--) // vertical offset içinde block (örneğin tsize-1'den 0'a)
                        {
                            // Orijinal verideki byte'ın indeksi: (satır indeksi * satır genişliği) + sütun byte indeksi
                            // Satır indeksi = (hangi block row'dayız * block yüksekliği) + block içindeki dikey offset
                            // Byte sütun indeksi = hangi block column'dayız (her block 1 byte genişliğinde)
                            int srcIndex = (br * Pktcar + vo) * widthBytes + bc;
                            byte currentByte = uncompressedPlanes[p][srcIndex];

                            // 68k'daki sıkıştırma mantığını simüle et: Önceki byte'tan farklı mı?
                            if (currentByte != prevBytes[p])
                            {
                                // Farklı ise, byte'ı ana veri buffer'ına ekle
                                mainData.Add(currentByte);
                                // Ve kontrol buffer'ında ilgili bit'i set et
                                controlBuffer[controlBufferIndex] |= (byte)(1 << controlBit);

                                // Önceki byte'ı güncelle
                                prevBytes[p] = currentByte;
                            }

                            // Kontrol biti pointer'ını ilerlet
                            controlBit--;
                            if (controlBit < 0)
                            {
                                controlBit = 7; // Biti resetle
                                controlBufferIndex++; // Sonraki kontrol byte'ına geç
                                                      // controlBuffer önceden boyutlandırıldı ve 0 ile başlatıldı, yeni byte eklemeye gerek yok.
                            }
                        }
                    }
                }
            }

            // 6. Kontrol Buffer'ını Sıkıştır (68k'daki comp2 rutini simülasyonu)
            // Bu kısım, ilk aşamada oluşturulan controlBuffer'ı RLE benzeri bir yöntemle sıkıştırır.
            List<byte> controlData = new List<byte>();         // Sıkıştırılmış kontrol byte'ları (68k comp2'de a5)
            List<byte> controlDataControl = new List<byte>();  // Sıkıştırılmış kontrol byte'larının kontrol bitleri (68k comp2'de a6)

            int cdControlIndex = 0; // controlDataControl içinde güncel byte indeksi
            int cdControlBit = 7;   // controlDataControl[cdControlIndex] içinde güncel bit konumu
            byte prevControlByte = 0; // Önceki kontrol byte değeri (68k comp2'deki d0)

            // Eğer sıkıştırılacak kontrol verisi varsa, controlDataControl'a ilk boş byte'ı ekle (68k'daki clr.b (a6))
            if (controlBufferSize > 0)
            {
                controlDataControl.Add(0);
            }

            // controlBuffer byte'ları üzerinde döngü yap
            for (int i = 0; i < controlBufferSize; i++)
            {
                byte currentControlByte = controlBuffer[i];

                // 68k'daki sıkıştırma mantığını simüle et: Önceki kontrol byte'tan farklı mı?
                if (currentControlByte != prevControlByte)
                {
                    // Farklı ise, kontrol byte'ını controlData'ya ekle
                    controlData.Add(currentControlByte);
                    // Ve controlDataControl buffer'ında ilgili bit'i set et
                    // Burada Add(0) yapıldıysa liste boş değil, güvenle erişebiliriz.
                    controlDataControl[cdControlIndex] |= (byte)(1 << cdControlBit);

                    // Önceki kontrol byte'ı güncelle
                    prevControlByte = currentControlByte;
                }

                // Kontrol biti pointer'ını ilerlet
                cdControlBit--;
                if (cdControlBit < 0)
                {
                    cdControlBit = 7; // Biti resetle
                    cdControlIndex++; // Sonraki controlDataControl byte'ına geç
                    controlDataControl.Add(0); // Yeni boş kontrol byte'ı ekle (68k'daki addq.l #1, a6; clr.b (a6))
                }
            }

            // 7. Nihai Sıkıştırılmış Veriyi Oluşturma
            int headerSize = 24; // 68k kodundaki başlık boyutu (BMCode + 6 kelime + 2 DWord)

            // Veri bölümlerinin offsetlerini hesapla (başlığın başından itibaren)
            int mainDataOffset = headerSize;
            int controlDataOffset = mainDataOffset + mainData.Count;
            int controlDataControlOffset = controlDataOffset + controlData.Count;

            // Sıkıştırılmış kontrol kontrol buffer'ının gerçek boyutu (List.Count, sondaki boş byte hariç olabilir mi?)
            // 68k kodunda a6'nın son hali PkDatas2'ye kadar olan veriyi temsil eder.
            // List<byte> kullanırken, son Add(0) kontrol bit döngüsü bittiğinde yapılmışsa, o byte listenin parçasıdır.
            // 68k kodundaki addq.l #1,a6 komutu, d1 -1 olduğunda *hemen* yapılır.
            // Eğer son dbra d2 loop iterasyonu bittiğinde d1 -1 olduysa, a6 bir kez daha artar ve yeni byte sıfırlanır.
            // Bu yeni boş byte da PkDatas2 offsetine dahil edilir. Bu yüzden List<byte>.Count muhtemelen doğrudur.

            int totalSize = controlDataControlOffset + controlDataControl.Count;

            // Toplam boyutu çift sayıya yuvarla (68k'daki AND $FFFFFFFE)
            if (totalSize % 2 != 0)
            {
                totalSize++;
            }

            byte[] compressedData = new byte[totalSize];

            // Başlığı Yaz (Big Endian formatında)
            WriteBigEndianDWord(compressedData, 0, BMCode);
            WriteBigEndianWord(compressedData, 4, (ushort)Pkdx);
            WriteBigEndianWord(compressedData, 6, (ushort)Pkdy);
            WriteBigEndianWord(compressedData, 8, (ushort)Pktx);
            WriteBigEndianWord(compressedData, 10, (ushort)Pkty);
            WriteBigEndianWord(compressedData, 12, (ushort)Pktcar);
            WriteBigEndianWord(compressedData, 14, (ushort)Pknplan);
            // Offsetler başlığın başından itibaren hesaplanır. MakePalette'teki -24, muhtemelen
            // fileData'nın tamamı yerine sadece pixelData kısmını işlediği içindir.
            // Biz tüm compressedData array'ini üretiyoruz, offsetler 0'dan başlar.
            WriteBigEndianDWord(compressedData, 16, (uint)controlDataOffset);       // PkPoint2 (Offset to controlData)
            WriteBigEndianDWord(compressedData, 20, (uint)controlDataControlOffset); // PkDatas2 (Offset to controlDataControl)

            // Veri Bölümlerini Kopyala
            mainData.ToArray().CopyTo(compressedData, mainDataOffset);
            controlData.ToArray().CopyTo(compressedData, controlDataOffset);
            controlDataControl.ToArray().CopyTo(compressedData, controlDataControlOffset);

            // Kalan padding byte'lar new byte[] tarafından zaten 0 olarak başlatılmıştır.

            return compressedData;
        }

        // Big Endian formatında 2 byte (Word) yazar
        private static void WriteBigEndianWord(byte[] buffer, int offset, ushort value)
        {
            if (offset + 1 >= buffer.Length) return;
            buffer[offset] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        // Big Endian formatında 4 byte (DWord) yazar
        private static void WriteBigEndianDWord(byte[] buffer, int offset, uint value)
        {
            if (offset + 3 >= buffer.Length) return;
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }





        /// ------------------------------
        /// 


        // Mevcut FindClosestPaletteIndex ve ColorDistance fonksiyonları buraya eklenebilir.
        private static double ColorDistance(Color c1, Color c2)
        {
            return Math.Sqrt(Math.Pow(c1.R - c2.R, 2) + Math.Pow(c1.G - c2.G, 2) + Math.Pow(c1.B - c2.B, 2));
        }

        private static int FindClosestPaletteIndex(Color inputColor, Color[] palette)
        {
            int bestIndex = 0;
            double minDistance = double.MaxValue;

            // reducedPalette yerine doğrudan AmigaPic paleti kullanılmalı (eğer öyleyse).
            // Burada parametre olarak gelen 'palette' kullanılır.
            for (int i = 0; i < palette.Length; i++)
            {
                Color paletteColor = palette[i];
                double distance = ColorDistance(inputColor, paletteColor);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }


        /// <summary>
        /// Bitmap'i Amiga bitplane formatına dönüştürür, her bitplane ayrı bir byte dizisi olarak.
        /// Quantizasyon (palete eşleme) yapılır. EHB modu desteklenir.
        /// </summary>
        /// <param name="image">Dönüştürülecek Bitmap.</param>
        /// <param name="bitplanes">Hedef bitplane sayısı (örneğin 5 veya 6).</param>
        /// <param name="palette">Kullanılacak palet.</param>
        /// <param name="widthBytes">Çıktı olarak her satırın byte cinsinden genişliği (16-bit hizalı).</param>
        /// <param name="heightLines">Çıktı olarak resmin satır sayısı.</param>
        /// <returns>Her bir bitplane'in byte verisini içeren byte dizileri dizisi.</returns>
        public static byte[][] ConvertBitmapToPlanar(Bitmap image, int bitplanes, Color[] palette, out int widthBytes, out int heightLines)
        {
            if (image == null || palette == null || bitplanes <= 0 || bitplanes > 8)
            {
                throw new ArgumentException("Invalid input parameters for ConvertBitmapToPlanar.");
            }

            heightLines = image.Height;
            // Amiga width must be divisible by 16 bits (2 bytes)
            // widthBytes = ((pixelWidth + 15) / 16) * 2;
            widthBytes = ((image.Width + 15) / 16) * 2;

            byte[][] planarData = new byte[bitplanes][];
            int planeSize = heightLines * widthBytes;
            for (int i = 0; i < bitplanes; i++)
            {
                planarData[i] = new byte[planeSize];
            }

            // Perform quantization (finding the closest palette index)
            // It's more efficient to do this once per pixel.
            int[,] quantizedPixels = new int[image.Width, image.Height];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    quantizedPixels[x, y] = FindClosestPaletteIndex(pixel, palette);
                }
                //Application.DoEvents(); // UI'yi güncellemek için, eğer UI thread'den çağrılıyorsa
            }


            // Lock bits for faster pixel access
            BitmapData bmpData = null;
            try
            {
                // Pixel formatını orijinal bitmap'in formatına göre ayarlayın.
                // Örneğin, Format24bppRgb veya Format32bppArgb yaygındır.
                // Güvenlik için orijinal formatı kullanmak en iyisidir, ancak eğer
                // Format32bppArgb'ye dönüştürmeyi tercih ediyorsanız, öncesinde bunu yapabilirsiniz.
                bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                        ImageLockMode.ReadOnly,
                                        image.PixelFormat); // Bitmap'in gerçek formatı kullanılmalı

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int stride = bmpData.Stride; // Bitmap'in satır uzunluğu (padding dahil)

                    for (int y = 0; y < heightLines; y++)
                    {
                        //byte[] rowData = new byte[widthBytes]; // Artık satır bazlı ara buffer'a gerek yok

                        for (int x = 0; x < image.Width; x++) // Orijinal Bitmap genişliği boyunca döngü
                        {
                            int colorIndex = quantizedPixels[x, y]; // Önceden hesaplanan indeksi kullan

                            int byteOffsetInScanline = x / 8;      // Scanline içindeki byte'ın offseti
                            int bitPositionInByte = 7 - (x % 8); // Byte içindeki bit konumu (MSB first)

                            // EHB modu kontrolü (6 bitplane ve indeks 31'den büyükse)
                            if (bitplanes == 6 && colorIndex > 31)
                            {
                                // Renk indeksi 32-63 arasındaysa, base palet indeksi 0-31 olur ve 6. bitplane set edilir.
                                // Base indeks ilk 5 bitplane'e yerleştirilir.
                                colorIndex -= 32; // Base 0-31 indeksi
                                                  // 6. bitplane'e (index 5) ilgili bit'i yerleştir
                                planarData[5][y * widthBytes + byteOffsetInScanline] |= (byte)(1 << bitPositionInByte);
                            }

                            // Ana bitplane'leri işle (ilk 5 bitplane, EHB modunda bile)
                            // Minimum bitplane sayısı ve 5 arasında küçük olanı alıyoruz,
                            // böylece 1-5 bitplane'li resimler de doğru işlenir.
                            for (int plane = 0; plane < Math.Min(bitplanes, 5); plane++)
                            {
                                // Renk indeksinin ilgili bitini kontrol et (0'dan 4'e kadar)
                                if (((colorIndex >> plane) & 1) != 0)
                                {
                                    // İlgili bit set ise, ilgili bitplane dizisinde ilgili bit'i set et
                                    planarData[plane][y * widthBytes + byteOffsetInScanline] |= (byte)(1 << bitPositionInByte);
                                }
                                // else bit is 0, byte is already initialized to 0
                            }
                        }

                        // Padding alanı için bitleri sıfırlama (isteğe bağlı ama iyi uygulama)
                        // Eğer Amiga genişliği orijinal genişlikten fazlaysa, padding alanını sıfırla
                        for (int x = image.Width; x < widthBytes * 8; x++)
                        {
                            int byteOffsetInScanline = x / 8;
                            int bitPositionInByte = 7 - (x % 8);
                            for (int plane = 0; plane < bitplanes; plane++)
                            {
                                // Zaten new byte[size] ile sıfırlandığı için ekstra bir işlem gerekmez.
                                // Sadece emin olmak için:
                                // planarData[plane][y * widthBytes + byteOffsetInScanline] &= (byte)~(1 << bitPositionInByte);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (bmpData != null)
                {
                    image.UnlockBits(bmpData);
                }
            }


            return planarData;
        }
    


}
}
