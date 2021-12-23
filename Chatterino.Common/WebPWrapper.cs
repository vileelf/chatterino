using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace Chatterino.Common
{
    [SuppressUnmanagedCodeSecurityAttribute]
    public sealed class WebPWrapper
    {

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        
        private static readonly int WEBP_DECODER_ABI_VERSION = 0x0208;
        
        public static int WebPConfigInit(ref WebPConfig config, WebPPreset preset, float quality)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPConfigInitInternal_x86(ref config, preset, quality, WEBP_DECODER_ABI_VERSION);
                case 8:
                    return WebPConfigInitInternal_x64(ref config, preset, quality, WEBP_DECODER_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigInitInternal")]
        private static extern int WebPConfigInitInternal_x86(ref WebPConfig config, WebPPreset preset, float quality, int WEBP_DECODER_ABI_VERSION);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigInitInternal")]
        private static extern int WebPConfigInitInternal_x64(ref WebPConfig config, WebPPreset preset, float quality, int WEBP_DECODER_ABI_VERSION);


        public static VP8StatusCode WebPGetFeatures(IntPtr rawWebP, int data_size, ref WebPBitstreamFeatures features)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPGetFeaturesInternal_x86(rawWebP, (UIntPtr)data_size, ref features, WEBP_DECODER_ABI_VERSION);
                case 8:
                    return WebPGetFeaturesInternal_x64(rawWebP, (UIntPtr)data_size, ref features, WEBP_DECODER_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetFeaturesInternal")]
        private static extern VP8StatusCode WebPGetFeaturesInternal_x86([InAttribute()] IntPtr rawWebP, UIntPtr data_size, ref WebPBitstreamFeatures features, int WEBP_DECODER_ABI_VERSION);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetFeaturesInternal")]
        private static extern VP8StatusCode WebPGetFeaturesInternal_x64([InAttribute()] IntPtr rawWebP, UIntPtr data_size, ref WebPBitstreamFeatures features, int WEBP_DECODER_ABI_VERSION);

        public static int WebPConfigLosslessPreset(ref WebPConfig config, int level)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPConfigLosslessPreset_x86(ref config, level);
                case 8:
                    return WebPConfigLosslessPreset_x64(ref config, level);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigLosslessPreset")]
        private static extern int WebPConfigLosslessPreset_x86(ref WebPConfig config, int level);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPConfigLosslessPreset")]
        private static extern int WebPConfigLosslessPreset_x64(ref WebPConfig config, int level);

        public static int WebPValidateConfig(ref WebPConfig config)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPValidateConfig_x86(ref config);
                case 8:
                    return WebPValidateConfig_x64(ref config);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPValidateConfig")]
        private static extern int WebPValidateConfig_x86(ref WebPConfig config);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPValidateConfig")]
        private static extern int WebPValidateConfig_x64(ref WebPConfig config);

        public static int WebPPictureInitInternal(ref WebPPicture wpic)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureInitInternal_x86(ref wpic, WEBP_DECODER_ABI_VERSION);
                case 8:
                    return WebPPictureInitInternal_x64(ref wpic, WEBP_DECODER_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureInitInternal")]
        private static extern int WebPPictureInitInternal_x86(ref WebPPicture wpic, int WEBP_DECODER_ABI_VERSION);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureInitInternal")]
        private static extern int WebPPictureInitInternal_x64(ref WebPPicture wpic, int WEBP_DECODER_ABI_VERSION);

        public static int WebPPictureImportBGR(ref WebPPicture wpic, IntPtr bgr, int stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureImportBGR_x86(ref wpic, bgr, stride);
                case 8:
                    return WebPPictureImportBGR_x64(ref wpic, bgr, stride);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGR")]
        private static extern int WebPPictureImportBGR_x86(ref WebPPicture wpic, IntPtr bgr, int stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGR")]
        private static extern int WebPPictureImportBGR_x64(ref WebPPicture wpic, IntPtr bgr, int stride);

        public static int WebPPictureImportBGRA(ref WebPPicture wpic, IntPtr bgra, int stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureImportBGRA_x86(ref wpic, bgra, stride);
                case 8:
                    return WebPPictureImportBGRA_x64(ref wpic, bgra, stride);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRA")]
        private static extern int WebPPictureImportBGRA_x86(ref WebPPicture wpic, IntPtr bgra, int stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRA")]
        private static extern int WebPPictureImportBGRA_x64(ref WebPPicture wpic, IntPtr bgra, int stride);

        public static int WebPPictureImportBGRX(ref WebPPicture wpic, IntPtr bgr, int stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureImportBGRX_x86(ref wpic, bgr, stride);
                case 8:
                    return WebPPictureImportBGRX_x64(ref wpic, bgr, stride);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRX")]
        private static extern int WebPPictureImportBGRX_x86(ref WebPPicture wpic, IntPtr bgr, int stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureImportBGRX")]
        private static extern int WebPPictureImportBGRX_x64(ref WebPPicture wpic, IntPtr bgr, int stride);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int WebPMemoryWrite([In()] IntPtr data, UIntPtr data_size, ref WebPPicture wpic);
        public static WebPMemoryWrite OnCallback;

        public static int WebPEncode(ref WebPConfig config, ref WebPPicture picture)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncode_x86(ref config, ref picture);
                case 8:
                    return WebPEncode_x64(ref config, ref picture);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncode")]
        private static extern int WebPEncode_x86(ref WebPConfig config, ref WebPPicture picture);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncode")]
        private static extern int WebPEncode_x64(ref WebPConfig config, ref WebPPicture picture);

        public static void WebPPictureFree(ref WebPPicture picture)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    WebPPictureFree_x86(ref picture);
                    break;
                case 8:
                    WebPPictureFree_x64(ref picture);
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureFree")]
        private static extern void WebPPictureFree_x86(ref WebPPicture wpic);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureFree")]
        private static extern void WebPPictureFree_x64(ref WebPPicture wpic);

        public static int WebPGetInfo(IntPtr data, int data_size, out int width, out int height)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPGetInfo_x86(data, (UIntPtr)data_size, out width, out height);
                case 8:
                    return WebPGetInfo_x64(data, (UIntPtr)data_size, out width, out height);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetInfo")]
        private static extern int WebPGetInfo_x86([InAttribute()] IntPtr data, UIntPtr data_size, out int width, out int height);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetInfo")]
        private static extern int WebPGetInfo_x64([InAttribute()] IntPtr data, UIntPtr data_size, out int width, out int height);

        public static void WebPDecodeBGRInto(IntPtr data, int data_size, IntPtr output_buffer, int output_buffer_size, int output_stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    if (WebPDecodeBGRInto_x86(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride) == null)
                        throw new InvalidOperationException("Can not decode WebP");
                    break;
                case 8:
                    if (WebPDecodeBGRInto_x64(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride) == null)
                        throw new InvalidOperationException("Can not decode WebP");
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRInto")]
        private static extern IntPtr WebPDecodeBGRInto_x86([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRInto")]
        private static extern IntPtr WebPDecodeBGRInto_x64([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

        public static void WebPDecodeBGRAInto(IntPtr data, int data_size, IntPtr output_buffer, int output_buffer_size, int output_stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    if (WebPDecodeBGRAInto_x86(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride) == null)
                        throw new InvalidOperationException("Can not decode WebP"); 
                    break;
                case 8:
                    if (WebPDecodeBGRAInto_x64(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride) == null)
                        throw new InvalidOperationException("Can not decode WebP");
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRAInto")]
        private static extern IntPtr WebPDecodeBGRAInto_x86([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeBGRAInto")]
        private static extern IntPtr WebPDecodeBGRAInto_x64([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

        public static void WebPDecodeARGBInto(IntPtr data, int data_size, IntPtr output_buffer, int output_buffer_size, int output_stride)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    if (WebPDecodeARGBInto_x86(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride) == null)
                        throw new InvalidOperationException("Can not decode WebP");
                    break;
                case 8:
                    if (WebPDecodeARGBInto_x64(data, (UIntPtr)data_size, output_buffer, output_buffer_size, output_stride) == null)
                        throw new InvalidOperationException("Can not decode WebP");
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeARGBInto")]
        private static extern IntPtr WebPDecodeARGBInto_x86([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecodeARGBInto")]
        private static extern IntPtr WebPDecodeARGBInto_x64([InAttribute()] IntPtr data, UIntPtr data_size, IntPtr output_buffer, int output_buffer_size, int output_stride);

        public static int WebPInitDecoderConfig(ref WebPDecoderConfig webPDecoderConfig)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPInitDecoderConfigInternal_x86(ref webPDecoderConfig, WEBP_DECODER_ABI_VERSION);
                case 8:
                    return WebPInitDecoderConfigInternal_x64(ref webPDecoderConfig, WEBP_DECODER_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPInitDecoderConfigInternal")]
        private static extern int WebPInitDecoderConfigInternal_x86(ref WebPDecoderConfig webPDecoderConfig, int WEBP_DECODER_ABI_VERSION);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPInitDecoderConfigInternal")]
        private static extern int WebPInitDecoderConfigInternal_x64(ref WebPDecoderConfig webPDecoderConfig, int WEBP_DECODER_ABI_VERSION);

        public static VP8StatusCode WebPDecode(IntPtr data, int data_size, ref WebPDecoderConfig webPDecoderConfig)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPDecode_x86(data, (UIntPtr)data_size, ref webPDecoderConfig);
                case 8:
                    return WebPDecode_x64(data, (UIntPtr)data_size, ref webPDecoderConfig);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecode")]
        private static extern VP8StatusCode WebPDecode_x86(IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDecode")]
        private static extern VP8StatusCode WebPDecode_x64(IntPtr data, UIntPtr data_size, ref WebPDecoderConfig config);

        public static void WebPFreeDecBuffer(ref WebPDecBuffer buffer)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    WebPFreeDecBuffer_x86(ref buffer);
                    break;
                case 8:
                    WebPFreeDecBuffer_x64(ref buffer);
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFreeDecBuffer")]
        private static extern void WebPFreeDecBuffer_x86(ref WebPDecBuffer buffer);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFreeDecBuffer")]
        private static extern void WebPFreeDecBuffer_x64(ref WebPDecBuffer buffer);

        public static int WebPEncodeBGR(IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncodeBGR_x86(bgr, width, height, stride, quality_factor, out output);
                case 8:
                    return WebPEncodeBGR_x64(bgr, width, height, stride, quality_factor, out output);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGR")]
        private static extern int WebPEncodeBGR_x86([InAttribute()] IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGR")]
        private static extern int WebPEncodeBGR_x64([InAttribute()] IntPtr bgr, int width, int height, int stride, float quality_factor, out IntPtr output);

        public static int WebPEncodeBGRA(IntPtr bgra, int width, int height, int stride, float quality_factor, out IntPtr output)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncodeBGRA_x86(bgra, width, height, stride, quality_factor, out output);
                case 8:
                    return WebPEncodeBGRA_x64(bgra, width, height, stride, quality_factor, out output);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGRA")]
        private static extern int WebPEncodeBGRA_x86([InAttribute()] IntPtr bgra, int width, int height, int stride, float quality_factor, out IntPtr output);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeBGRA")]
        private static extern int WebPEncodeBGRA_x64([InAttribute()] IntPtr bgra, int width, int height, int stride, float quality_factor, out IntPtr output);

        public static int WebPEncodeLosslessBGR(IntPtr bgr, int width, int height, int stride, out IntPtr output)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncodeLosslessBGR_x86(bgr, width, height, stride, out output);
                case 8:
                    return WebPEncodeLosslessBGR_x64(bgr, width, height, stride, out output);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGR")]
        private static extern int WebPEncodeLosslessBGR_x86([InAttribute()] IntPtr bgr, int width, int height, int stride, out IntPtr output);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGR")]
        private static extern int WebPEncodeLosslessBGR_x64([InAttribute()] IntPtr bgr, int width, int height, int stride, out IntPtr output);

        public static int WebPEncodeLosslessBGRA(IntPtr bgra, int width, int height, int stride, out IntPtr output)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPEncodeLosslessBGRA_x86(bgra, width, height, stride, out output);
                case 8:
                    return WebPEncodeLosslessBGRA_x64(bgra, width, height, stride, out output);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGRA")]
        private static extern int WebPEncodeLosslessBGRA_x86([InAttribute()] IntPtr bgra, int width, int height, int stride, out IntPtr output);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPEncodeLosslessBGRA")]
        private static extern int WebPEncodeLosslessBGRA_x64([InAttribute()] IntPtr bgra, int width, int height, int stride, out IntPtr output);

        public static void WebPFree(IntPtr p)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    WebPFree_x86(p);
                    break;
                case 8:
                    WebPFree_x64(p);
                    break;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFree")]
        private static extern void WebPFree_x86(IntPtr p);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPFree")]
        private static extern void WebPFree_x64(IntPtr p);

        public static int WebPGetDecoderVersion()
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPGetDecoderVersion_x86();
                case 8:
                    return WebPGetDecoderVersion_x64();
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_x86();
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPGetDecoderVersion")]
        private static extern int WebPGetDecoderVersion_x64();

        public static int WebPPictureDistortion(ref WebPPicture srcPicture, ref WebPPicture refPicture, int metric_type, IntPtr pResult)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPPictureDistortion_x86(ref srcPicture, ref refPicture, metric_type, pResult);
                case 8:
                    return WebPPictureDistortion_x64(ref srcPicture, ref refPicture, metric_type, pResult);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        [DllImport("libwebp_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureDistortion")]
        private static extern int WebPPictureDistortion_x86(ref WebPPicture srcPicture, ref WebPPicture refPicture, int metric_type, IntPtr pResult);
        [DllImport("libwebp_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPPictureDistortion")]
        private static extern int WebPPictureDistortion_x64(ref WebPPicture srcPicture, ref WebPPicture refPicture, int metric_type, IntPtr pResult);
        
        
        
        //animated functions
        const int WEBP_DEMUX_ABI_VERSION = 0x0107;
        [DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderOptionsInitInternal")]
        private static extern int WebPAnimDecoderOptionsInit_x86(ref WebPAnimDecoderOptions decopts, int abiversion);
        [DllImport("libwebpdemux_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderOptionsInitInternal")]
        private static extern int WebPAnimDecoderOptionsInit_x64(ref WebPAnimDecoderOptions decopts, int abiversion);
        
        public static int WebPAnimDecoderOptionsInit(ref WebPAnimDecoderOptions decopts)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPAnimDecoderOptionsInit_x86(ref decopts, WEBP_DEMUX_ABI_VERSION);
                case 8:
                    return WebPAnimDecoderOptionsInit_x64(ref decopts, WEBP_DEMUX_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        
        [DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderNewInternal")]
        private static extern IntPtr WebPAnimDecoderNew_x86(ref WebPData webpdata , ref WebPAnimDecoderOptions decopts, int abiversion);
        [DllImport("libwebpdemux_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderNewInternal")]
        private static extern IntPtr WebPAnimDecoderNew_x64(ref WebPData webpdata , ref WebPAnimDecoderOptions decopts, int abiversion);
        
        public static IntPtr WebPAnimDecoderNew(ref WebPData webpdata , ref WebPAnimDecoderOptions decopts)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPAnimDecoderNew_x86(ref webpdata, ref decopts, WEBP_DEMUX_ABI_VERSION);
                case 8:
                    return WebPAnimDecoderNew_x64(ref webpdata, ref decopts, WEBP_DEMUX_ABI_VERSION);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        
        [DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderGetInfo")]
        private static extern int WebPAnimDecoderGetInfo_x86(IntPtr decoder, ref WebPAnimInfo animinfo);
        [DllImport("libwebpdemux_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderGetInfo")]
        private static extern int WebPAnimDecoderGetInfo_x64(IntPtr decoder, ref WebPAnimInfo animinfo);
        
        public static int WebPAnimDecoderGetInfo(IntPtr decoder, ref WebPAnimInfo animinfo)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPAnimDecoderGetInfo_x86(decoder, ref animinfo);
                case 8:
                    return WebPAnimDecoderGetInfo_x64(decoder, ref animinfo);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        
        [DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderHasMoreFrames")]
        private static extern int WebPAnimDecoderHasMoreFrames_x86(IntPtr decoder);
        [DllImport("libwebpdemux_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderHasMoreFrames")]
        private static extern int WebPAnimDecoderHasMoreFrames_x64(IntPtr decoder);
        
        public static int WebPAnimDecoderHasMoreFrames(IntPtr decoder)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPAnimDecoderHasMoreFrames_x86(decoder);
                case 8:
                    return WebPAnimDecoderHasMoreFrames_x64(decoder);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        
        [DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderGetNext")]
        private static extern int WebPAnimDecoderGetNext_x86(IntPtr decoder, out IntPtr outbuf, out int timestamp);
        [DllImport("libwebpdemux_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderGetNext")]
        private static extern int WebPAnimDecoderGetNext_x64(IntPtr decoder, out IntPtr outbuf, out int timestamp);
        
        public static int WebPAnimDecoderGetNext(IntPtr decoder, out IntPtr outbuf, out int timestamp)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return WebPAnimDecoderGetNext_x86(decoder, out outbuf, out timestamp);
                case 8:
                    return WebPAnimDecoderGetNext_x64(decoder, out outbuf, out timestamp);
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        
        [DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderReset")]
        private static extern void WebPAnimDecoderReset_x86(IntPtr decoder);
        [DllImport("libwebpdemux_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderReset")]
        private static extern void WebPAnimDecoderReset_x64(IntPtr decoder);
        
        public static void WebPAnimDecoderReset(IntPtr decoder)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    WebPAnimDecoderReset_x86(decoder);
                    return;
                case 8:
                    WebPAnimDecoderReset_x64(decoder);
                    return;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
        
        [DllImport("libwebpdemux_x86.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderDelete")]
        private static extern void WebPAnimDecoderDelete_x86(IntPtr decoder);
        [DllImport("libwebpdemux_x64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderDelete")]
        private static extern void WebPAnimDecoderDelete_x64(IntPtr decoder);
        public static void WebPAnimDecoderDelete(IntPtr decoder)
        {
            switch (IntPtr.Size)
            {
                case 4:
                    WebPAnimDecoderDelete_x86(decoder);
                    return;
                case 8:
                    WebPAnimDecoderDelete_x64(decoder);
                    return;
                default:
                    throw new InvalidOperationException("Invalid platform. Can not find proper function");
            }
        }
    }
    
    public enum WebPPreset
    {
        WEBP_PRESET_DEFAULT = 0,
        WEBP_PRESET_PICTURE,
        WEBP_PRESET_PHOTO,
        WEBP_PRESET_DRAWING,
        WEBP_PRESET_ICON,
        WEBP_PRESET_TEXT
    };

    public enum WebPEncodingError
    {
        VP8_ENC_OK = 0,
        VP8_ENC_ERROR_OUT_OF_MEMORY,
        VP8_ENC_ERROR_BITSTREAM_OUT_OF_MEMORY,
        VP8_ENC_ERROR_NULL_PARAMETER,
        VP8_ENC_ERROR_INVALID_CONFIGURATION,
        VP8_ENC_ERROR_BAD_DIMENSION,
        VP8_ENC_ERROR_PARTITION0_OVERFLOW,
        VP8_ENC_ERROR_PARTITION_OVERFLOW,
        VP8_ENC_ERROR_BAD_WRITE,
        VP8_ENC_ERROR_FILE_TOO_BIG,
        VP8_ENC_ERROR_USER_ABORT,
        VP8_ENC_ERROR_LAST,
    }

    public enum VP8StatusCode
    {
        VP8_STATUS_OK = 0,
        VP8_STATUS_OUT_OF_MEMORY,
        VP8_STATUS_INVALID_PARAM,
        VP8_STATUS_BITSTREAM_ERROR,
        VP8_STATUS_UNSUPPORTED_FEATURE,
        VP8_STATUS_SUSPENDED,
        VP8_STATUS_USER_ABORT,
        VP8_STATUS_NOT_ENOUGH_DATA,
    }

    public enum WebPImageHint
    {
        WEBP_HINT_DEFAULT = 0,
        WEBP_HINT_PICTURE,
        WEBP_HINT_PHOTO,
        WEBP_HINT_GRAPH,
        WEBP_HINT_LAST
    };

    public enum WEBP_CSP_MODE
    {
        MODE_RGB = 0,
        MODE_RGBA = 1,
        MODE_BGR = 2,
        MODE_BGRA = 3,
        MODE_ARGB = 4,
        MODE_RGBA_4444 = 5,
        MODE_RGB_565 = 6,
        MODE_rgbA = 7,
        MODE_bgrA = 8,
        MODE_Argb = 9,
        MODE_rgbA_4444 = 10,
        MODE_YUV = 11,
        MODE_YUVA = 12,
        MODE_LAST = 13,
    }

    public enum DecState
    {
        STATE_WEBP_HEADER,
        STATE_VP8_HEADER,
        STATE_VP8_PARTS0,
        STATE_VP8_DATA,
        STATE_VP8L_HEADER,
        STATE_VP8L_DATA,
        STATE_DONE,
        STATE_ERROR
    };
    
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPAnimDecoderOptions {
        // Output colorspace. Only the following modes are supported:
        // MODE_RGBA, MODE_BGRA, MODE_rgbA and MODE_bgrA.
        public WEBP_CSP_MODE color_mode;
        public int use_threads;           // If true, use multi-threaded decoding.
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 7, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad;       // Padding for later use.
    };
    
    // Global information about the animation..
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPAnimInfo {
        public UInt32 canvas_width;
        public UInt32 canvas_height;
        public UInt32 loop_count;
        public UInt32 bgcolor;
        public UInt32 frame_count;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad;
    };
    
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPData {
        public IntPtr bytes;
        public UIntPtr size;
    };
    
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPBitstreamFeatures
    {
        public int Width;
        public int Height;
        public int Has_alpha;
        public int Has_animation;
        public int Format;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 5, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad;
    };

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPConfig
    {
        public int lossless;
        public float quality;
        public int method;
        public WebPImageHint image_hint;
        public int target_size;
        public float target_PSNR;
        public int segments;
        public int sns_strength;
        public int filter_strength;
        public int filter_sharpness;
        public int filter_type;
        public int autofilter;
        public int alpha_compression;
        public int alpha_filtering;
        public int alpha_quality;
        public int pass;
        public int show_compressed;
        public int preprocessing;
        public int partitions;
        public int partition_limit;
        public int emulate_jpeg_size;
        public int thread_level;
        public int low_memory;
        public int near_lossless;
        public int exact;
        public int delta_palettization;
        public int use_sharp_yuv;
        private readonly int pad1;
        private readonly int pad2;
    };

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPPicture
    {
        public int use_argb;
        public UInt32 colorspace;
        public int width;
        public int height;
        public IntPtr y;
        public IntPtr u;
        public IntPtr v;
        public int y_stride;
        public int uv_stride;
        public IntPtr a;
        public int a_stride;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad1;
        public IntPtr argb;
        public int argb_stride;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad2;
        public IntPtr writer;
        public IntPtr custom_ptr;
        // map for extra information (only for lossy compression mode)
        public int extra_info_type;
        public IntPtr extra_info;
        public IntPtr stats;
        public UInt32 error_code;
        public IntPtr progress_hook;
        public IntPtr user_data;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 13, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad3;
        private readonly IntPtr memory_;
        private readonly IntPtr memory_argb_;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad4;
    };

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPAuxStats
    {
        public int coded_size;
        public float PSNRY;
        public float PSNRU;
        public float PSNRV;
        public float PSNRALL;
        public float PSNRAlpha;
        public int block_count_intra4;
        public int block_count_intra16;
        public int block_count_skipped;
        public int header_bytes;
        public int mode_partition_0;
        public int residual_bytes_DC_segments0;
        public int residual_bytes_AC_segments0;
        public int residual_bytes_uv_segments0;
        public int residual_bytes_DC_segments1;
        public int residual_bytes_AC_segments1;
        public int residual_bytes_uv_segments1;
        public int residual_bytes_DC_segments2;
        public int residual_bytes_AC_segments2;
        public int residual_bytes_uv_segments2;
        public int residual_bytes_DC_segments3;
        public int residual_bytes_AC_segments3;
        public int residual_bytes_uv_segments3;
        public int segment_size_segments0;
        public int segment_size_segments1;
        public int segment_size_segments2;
        public int segment_size_segments3;
        public int segment_quant_segments0;
        public int segment_quant_segments1;
        public int segment_quant_segments2;
        public int segment_quant_segments3;
        public int segment_level_segments0;
        public int segment_level_segments1;
        public int segment_level_segments2;
        public int segment_level_segments3;
        public int alpha_data_size;
        public int layer_data_size;
        // lossless encoder statistics
        public Int32 lossless_features;
        public int histogram_bits;
        public int transform_bits;
        public int cache_bits;
        public int palette_size;
        public int lossless_size;
        public int lossless_hdr_size;
        public int lossless_data_size;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.U4)]
        private readonly uint[] pad;
    };

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPDecoderConfig
    {
        public WebPBitstreamFeatures input;
        public WebPDecBuffer output;
        public WebPDecoderOptions options;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPDecBuffer
    {
        public WEBP_CSP_MODE colorspace;
        public int width;
        public int height;
        public int is_external_memory;
        public RGBA_YUVA_Buffer u;
        private readonly UInt32 pad1;
        private readonly UInt32 pad2;
        private readonly UInt32 pad3;
        private readonly UInt32 pad4;
        public IntPtr private_memory;
    }

    [StructLayoutAttribute(LayoutKind.Explicit)]
    public struct RGBA_YUVA_Buffer
    {
        [FieldOffsetAttribute(0)]
        public WebPRGBABuffer RGBA;

        [FieldOffsetAttribute(0)]
        public WebPYUVABuffer YUVA;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPYUVABuffer
    {
        public IntPtr y;
        public IntPtr u;
        public IntPtr v;
        public IntPtr a;
        public int y_stride;
        public int u_stride;
        public int v_stride;
        public int a_stride;
        public UIntPtr y_size;
        public UIntPtr u_size;
        public UIntPtr v_size;
        public UIntPtr a_size;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct WebPRGBABuffer
    {
        public IntPtr rgba;
        public int stride;
        public UIntPtr size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WebPDecoderOptions
    {
        public int bypass_filtering;
        public int no_fancy_upsampling;
        public int use_cropping;
        public int crop_left;
        public int crop_top;
        public int crop_width;
        public int crop_height;
        public int use_scaling;
        public int scaled_width;
        public int scaled_height;
        public int use_threads;
        public int dithering_strength;
        public int flip;
        public int alpha_dithering_strength;
        private readonly UInt32 pad1;
        private readonly UInt32 pad2;
        private readonly UInt32 pad3;
        private readonly UInt32 pad4;
        private readonly UInt32 pad5;
    };
}