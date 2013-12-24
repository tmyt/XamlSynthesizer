#pragma once

#include "pch.h"

#define Property(_Type, _Name) \
	private: _Type m##_Name; \
	public: property _Type _Name { \
		_Type get() { return m##_Name;  } \
		void set(_Type value) { m##_Name = value; } \
	}

namespace Media
{
	ref class AudioRenderer;
	class TimerHandler;

	public delegate void SampleRequestedEventHandler(AudioRenderer^ renderer);

	public ref class AudioRenderer sealed
	{
	private:
		Microsoft::WRL::ComPtr<IXAudio2> pXAudio2;
		IXAudio2MasteringVoice* pMasteringVoice;
		IXAudio2SourceVoice* pSourceVoice;

		bool mEndOfStreamChecked;

		Windows::System::Threading::ThreadPoolTimer^ timer;

		void TimerHandler(Windows::System::Threading::ThreadPoolTimer^ timer);

		friend class PeriodicTimerHandler;

	public:
		AudioRenderer();

		void Play();
		void Stop();
		void AppendBuffer(const Platform::Array<uint8>^ samples, bool endOfStream);

		event SampleRequestedEventHandler^ SampleRequested;

		Property(int, Channels);
		Property(int, SamplesPerSec);
		Property(int, BitsPerSample);
		
		Property(bool, IsPlaying);

	};

	class PeriodicTimerHandler
	{
	private:
		AudioRenderer^ mRenderer;

	public:
		PeriodicTimerHandler(AudioRenderer^ renderer) : mRenderer(renderer) {}

		void operator()(Windows::System::Threading::ThreadPoolTimer^ source)
		{
			mRenderer->TimerHandler(source);
		}
	};
}