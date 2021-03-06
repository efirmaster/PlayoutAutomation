// This is the main DLL file.
#include "stdafx.h"
#include "FFMpegUtils.h"

namespace TAS {
	namespace FFMpegUtils {

		// unmanaged opbject
		_FFMpegWrapper::_FFMpegWrapper(char* fileName)
		{
			av_register_all();
			av_log_set_level(AV_LOG_DEBUG);
			pFormatCtx=NULL;
			if (avformat_open_input(&pFormatCtx, fileName, NULL, NULL) == 0)
				avformat_find_stream_info(pFormatCtx, NULL);
		};

		_FFMpegWrapper::~_FFMpegWrapper()
		{
			if (pFormatCtx)
				avformat_close_input(&pFormatCtx); 
		};

		int64_t _FFMpegWrapper::getFrameCount()
		{
			if (pFormatCtx)
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
					if(pFormatCtx->streams[i]->codecpar->codec_type==AVMEDIA_TYPE_VIDEO) 
						if (pFormatCtx->streams[i]->nb_frames > 0)
								return pFormatCtx->streams[i]->nb_frames; 
			// if not found
			return 0; 
		}

		int64_t _FFMpegWrapper::countFrames(unsigned int streamIndex)
		{
			int64_t frameCount = 0;
			if (pFormatCtx)
			{
				AVPacket *pOutput;
				pOutput = (AVPacket *) av_malloc(sizeof(AVPacket));
				while (av_read_frame(pFormatCtx, pOutput) >= 0)
				{
					if (pOutput->stream_index == streamIndex)
						frameCount++;
					av_free_packet(pOutput);
				}
			}
			return frameCount; 
		}

		int64_t _FFMpegWrapper::getAudioDuration()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
					if(pFormatCtx->streams[i]->codecpar->codec_type==AVMEDIA_TYPE_AUDIO) 
						//result is in AV_TIME_BASE units
						return (pFormatCtx->streams[i]->duration * pFormatCtx->streams[i]->time_base.num * AV_TIME_BASE) / pFormatCtx->streams[i]->time_base.den; 
			} 
			// if not found
			return 0; 
		}

		int _FFMpegWrapper::getHeight()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codec->height;
				} 
			}
			return 0; 
		}
		
		int _FFMpegWrapper::getWidth()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					if(pFormatCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO) 
						return pFormatCtx->streams[i]->codec->width;
				} 
			}
			return 0; 
		}

		AVFrame* _FFMpegWrapper::decodeFirstFrame()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i < pFormatCtx->nb_streams; i++)
				{
					AVCodecContext *codecCtx = pFormatCtx->streams[i]->codec;
					if (codecCtx->codec_type == AVMEDIA_TYPE_VIDEO)
					{
						AVFrame *picture = av_frame_alloc();
						AVPacket *packet = (AVPacket *)av_malloc(sizeof(AVPacket));
						AVCodec *pCodec = avcodec_find_decoder(codecCtx->codec_id);
						try
						{
							if (avcodec_open2(codecCtx, pCodec, NULL) < 0)
							{
								av_frame_free(&picture);
								return NULL; // unable to open coden
							}
							bool readSuccess = true;
							int frameFinished = 0;
							int bytesDecoded = 0;
							do
							{
								readSuccess = (av_read_frame(pFormatCtx, packet) == 0);
								if (readSuccess
									&& packet->stream_index == i
									&& packet->size > 0
									&& (bytesDecoded = avcodec_decode_video2(codecCtx, picture, &frameFinished, packet)) > 0)
								{
									if (frameFinished)
										return picture;
								}
							} while (!frameFinished && readSuccess);
							av_frame_free(&picture);
							return NULL;
						}
						finally
						{
							avcodec_close(codecCtx);
							av_free_packet(packet);
						}
					}
				}
			}
			return NULL;
		}

		AVFieldOrder _FFMpegWrapper::getFieldOrder()
		{
			if (pFormatCtx)
			{
				for (unsigned int i=0; i<pFormatCtx->nb_streams; i++)
				{
					AVCodecContext *codecCtx = pFormatCtx->streams[i]->codec;
					if (codecCtx->codec_type == AVMEDIA_TYPE_VIDEO)
					{
						if (codecCtx->field_order == AV_FIELD_UNKNOWN)
						{
							AVFrame *picture = av_frame_alloc();
							AVPacket *packet = (AVPacket *)av_malloc(sizeof(AVPacket));
							AVCodec *pCodec = avcodec_find_decoder(codecCtx->codec_id);
							try
							{
								if (avcodec_open2(codecCtx, pCodec, NULL) < 0)
									return AV_FIELD_UNKNOWN; // unable to open coden
								bool readSuccess = true;
								int frameFinished = 0;
								int bytesDecoded = 0;
								do
								{
									readSuccess = (av_read_frame(pFormatCtx, packet) == 0);
									if (readSuccess
										&& packet->stream_index == i
										&& packet->size > 0
										&& (bytesDecoded = avcodec_decode_video2(codecCtx, picture, &frameFinished, packet)) > 0)
									{
										if (frameFinished)
										{
											if (picture->interlaced_frame)
												if (picture->top_field_first)
													return AV_FIELD_TT;
												else
													return AV_FIELD_BB;
											else
												return AV_FIELD_PROGRESSIVE;
										}
									}
								} while (!frameFinished && readSuccess);
								return AV_FIELD_UNKNOWN;
							}
							finally
							{
								avcodec_close(codecCtx);
								av_frame_free(&picture);
								av_free_packet(packet);
							}
						}
						else
						return codecCtx->field_order;
					}
				} 
			}
			return AV_FIELD_UNKNOWN;
		}

		AVRational _FFMpegWrapper::getSAR()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i<pFormatCtx->nb_streams; i++)
				{
					if (pFormatCtx->streams[i]->codec->codec_type == AVMEDIA_TYPE_VIDEO)
						return pFormatCtx->streams[i]->sample_aspect_ratio;
				}
			}
			return av_make_q(0, 0);
		}

		AVRational _FFMpegWrapper::getFrameRate()
		{
			if (pFormatCtx)
			{
				for (unsigned int i = 0; i<pFormatCtx->nb_streams; i++)
				{
					if (pFormatCtx->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO)
						return av_stream_get_r_frame_rate(pFormatCtx->streams[i]);
				}
			}
			return av_make_q(0, 0);
		}

		int _FFMpegWrapper::getStreamCount()
		{
			if (pFormatCtx)
				return pFormatCtx->nb_streams;
			return 0;
		}

		StreamInfo ^ _FFMpegWrapper::getStreamInfo(unsigned int streamIndex) 
		{
			StreamInfo ^ ret = gcnew StreamInfo();
			if (pFormatCtx && pFormatCtx->nb_streams > streamIndex)
			{
				ret->StreamType = (StreamType)pFormatCtx->streams[streamIndex]->codec->codec_type;
				ret->Id = pFormatCtx->streams[streamIndex]->id;
				ret->Index = pFormatCtx->streams[streamIndex]->index;
				ret->ChannelCount = pFormatCtx->streams[streamIndex]->codecpar->channels;
			}
			return ret;
		}

		// managed object

		FFMpegWrapper::FFMpegWrapper(String^ fileName)
		{
			_fileName = fileName;
			char* fn = (char*)Marshal::StringToHGlobalAnsi(fileName).ToPointer();
			wrapper = new _FFMpegWrapper(fn);
			Marshal::FreeHGlobal(IntPtr((void*)fn));
		};

		FFMpegWrapper::~FFMpegWrapper()
		{
			delete wrapper;
		};

		Int64 FFMpegWrapper::GetFrameCount()
		{
			return wrapper->getFrameCount();
		}

		TimeSpan^ FFMpegWrapper::GetAudioDuration()
		{
			return gcnew TimeSpan(wrapper->getAudioDuration() * 10);
		}

		int FFMpegWrapper::GetHeight()
		{
			return wrapper->getHeight();
		}

		int FFMpegWrapper::GetWidth()
		{
			return wrapper->getWidth();
		}

		FieldOrder FFMpegWrapper::GetFieldOrder()
		{
			return (FieldOrder)(wrapper->getFieldOrder());
		}

		Rational^ FFMpegWrapper::GetSAR()
		{
			AVRational val = wrapper->getSAR();
			Rational ^ ret = gcnew Rational();
			ret->Num = val.num;
			ret->Den = val.den;
			return ret;
		}

		Rational^ FFMpegWrapper::GetFrameRate()
		{
			AVRational val = wrapper->getFrameRate();
			Rational ^ ret = gcnew Rational();
			ret->Num = val.num;
			ret->Den = val.den;
			return ret;
		}

		array<StreamInfo^>^ FFMpegWrapper::GetStreamInfo()
		{
			auto ret = gcnew array<StreamInfo^>(wrapper->getStreamCount());
			for (int i = 0; i < ret->Length; i++)
				ret[i] = wrapper->getStreamInfo(i);
			return ret;
		}


	}
}
