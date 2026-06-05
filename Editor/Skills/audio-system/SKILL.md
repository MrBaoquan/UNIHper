---
name: audio-system
description: 'Play music and sound effects using UNIHper AudioManager. Use when asked to play background music, trigger sound effects, control audio volume, or manage audio playback with MusicPlayer and EffectPlayer.'
---

# 音频系统

通过 `Managements.Audio` 访问。支持多通道音乐和音效播放。

## 播放音乐

```csharp
// 通过资源名播放（从 resources.json 加载 AudioClip）
Managements.Audio.PlayMusic("BGM_Main");
Managements.Audio.PlayMusic("BGM_Main", InVolume: 0.8f, bLoop: true);

// 通过 AudioClip 播放
Managements.Audio.PlayMusic(myClip, InVolume: 1.0f, bLoop: true);

// 多通道：Index 参数指定通道（默认 0）
Managements.Audio.PlayMusic("BGM_Battle", Index: 1);

// 控制
Managements.Audio.PauseMusic();       // 暂停通道 0
Managements.Audio.StopMusic();        // 停止通道 0
Managements.Audio.PlayMusic();        // 恢复已暂停的音乐
Managements.Audio.PauseMusic(1);      // 暂停通道 1
```

## 播放音效

```csharp
// 通过资源名（PlayOneShot，可叠加播放）
Managements.Audio.PlayEffect("ButtonClick");
Managements.Audio.PlayEffect("Explosion", volume: 0.5f);

// 多通道
Managements.Audio.PlayEffect("Hit", index: 1);
Managements.Audio.StopEffect();       // 停止通道 0
```

## 直接访问 AudioPlayer

```csharp
// MusicPlayer / EffectPlayer 是 AudioPlayer 组件
var musicSource = Managements.Audio.MusicPlayer.GetAudioSource(0);
musicSource.volume = 0.5f;
musicSource.pitch = 1.2f;

// 获取音频信息
float duration = Managements.Audio.MusicPlayer.Duration(0);
AudioClip clip = Managements.Audio.EffectPlayer.Clip(0);
```

## 重要提示

1. 音频资源需在 `resources.json` 中配置（参考 `resource-management` skill）
2. `PlayMusic` 替换当前通道正在播放的音乐；`PlayEffect` 使用 `PlayOneShot` 可叠加
3. 多通道通过 `Index` 参数区分，每个通道是独立的 `AudioSource`
