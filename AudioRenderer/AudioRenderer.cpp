#include"pch.h"

#include"AudioRenderer.h"

#pragma comment(lib, "XAudio2.lib")

using namespace Windows::Foundation;
using namespace Windows::System::Threading;

#define ThrowIfFailed(_Expr) do { auto hr = (_Expr); __abi_ThrowIfFailed(hr); } while(0)

#define usecs(X) ((X) * 10)
#define msecs(X) (usecs(X) * 1000)
#define secs(X) (msecs(X) * 1000)

namespace Media
{
	AudioRenderer::AudioRenderer()
	{
		ThrowIfFailed(XAudio2Create(&pXAudio2));

		ThrowIfFailed(pXAudio2->CreateMasteringVoice(&pMasteringVoice));
	}

	void AudioRenderer::TimerHandler(Windows::System::Threading::ThreadPoolTimer^ timer)
	{
		XAUDIO2_VOICE_STATE state;
		pSourceVoice->GetState(&state, 0);
		if (state.BuffersQueued < 2){
			SampleRequested(this);
		}
		if (state.BuffersQueued == 0 && mEndOfStreamChecked){
			ThrowIfFailed(pSourceVoice->Stop());
		}
	}

	void AudioRenderer::Play()
	{
		if (IsPlaying) return;
		IsPlaying = true;

		// Create a source voice
		WAVEFORMATEX waveformat;
		waveformat.wFormatTag = WAVE_FORMAT_PCM;
		waveformat.nChannels = Channels;
		waveformat.nSamplesPerSec = SamplesPerSec;
		waveformat.nAvgBytesPerSec = SamplesPerSec * (BitsPerSample / 8) * Channels;
		waveformat.nBlockAlign = Channels * BitsPerSample / 8;
		waveformat.wBitsPerSample = BitsPerSample;
		waveformat.cbSize = 0;

		ThrowIfFailed(pXAudio2->CreateSourceVoice(&pSourceVoice, &waveformat));
		// Start the source voice
		ThrowIfFailed(pSourceVoice->Start());
		mEndOfStreamChecked = false;

		// Set Stream event wather
		TimeSpan oneSec;
		oneSec.Duration = msecs(500);
		auto handler = ref new TimerElapsedHandler(PeriodicTimerHandler(this));
		timer = ThreadPoolTimer::CreatePeriodicTimer(handler, oneSec);
	}

	void AudioRenderer::Stop()
	{
		if (!IsPlaying) return;
		IsPlaying = false;
		// Cancel timer
		timer->Cancel();
		// Stop stream
		ThrowIfFailed(pSourceVoice->Stop());
	}

	void AudioRenderer::AppendBuffer(const Platform::Array<uint8>^ samples, bool endOfStream)
	{
		// Create a button to reference the byte array
		XAUDIO2_BUFFER buffer = { 0 };
		buffer.AudioBytes = samples->Length;
		buffer.pAudioData = samples->Data;
		if (endOfStream){
			buffer.Flags = XAUDIO2_END_OF_STREAM;
			mEndOfStreamChecked = true;
		}
		// Submit the buffer
		ThrowIfFailed(pSourceVoice->SubmitSourceBuffer(&buffer));
	}
}