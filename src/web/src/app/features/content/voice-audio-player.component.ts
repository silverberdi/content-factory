import { Component, ElementRef, Input, OnDestroy, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-voice-audio-player',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-3.5 rounded-xl bg-slate-950/80 border border-slate-800/90 text-slate-100 space-y-2.5">
      <!-- Hidden Audio Element -->
      <audio #audioElement
             [src]="audioUrl"
             (timeupdate)="onTimeUpdate()"
             (loadedmetadata)="onLoadedMetadata()"
             (ended)="onEnded()"
             (error)="onError()"
             preload="metadata">
      </audio>

      <!-- Player Top Bar: Play/Pause Button, Timers, Playback Rate -->
      <div class="flex items-center justify-between gap-3">
        <div class="flex items-center gap-2.5">
          <!-- Play / Pause Toggle -->
          <button (click)="togglePlay()"
                  type="button"
                  [disabled]="hasError()"
                  class="w-8 h-8 rounded-lg flex items-center justify-center transition-colors shadow-xs"
                  [ngClass]="{
                    'bg-violet-600 hover:bg-violet-500 text-white cursor-pointer': !hasError(),
                    'bg-slate-800 text-slate-500 cursor-not-allowed': hasError()
                  }"
                  [title]="isPlaying() ? 'Pausar' : 'Reproducir'">
            <i class="pi text-xs" [ngClass]="isPlaying() ? 'pi-pause' : 'pi-play'"></i>
          </button>

          <!-- Current / Total Duration -->
          <div class="font-mono text-xs text-slate-300">
            <span>{{ formatTime(currentTime()) }}</span>
            <span class="text-slate-500 mx-1">/</span>
            <span>{{ formatTime(totalDuration()) }}</span>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <!-- Speed Toggle -->
          <button (click)="cyclePlaybackRate()"
                  type="button"
                  class="px-2 py-0.5 rounded text-[11px] font-mono font-bold bg-slate-900 hover:bg-slate-800 border border-slate-700 text-violet-300 cursor-pointer transition-colors"
                  title="Velocidad de reproducción">
            {{ playbackRate() }}x
          </button>

          <!-- Format Tag -->
          <span class="px-1.5 py-0.5 rounded text-[10px] font-mono bg-violet-950/60 border border-violet-500/30 text-violet-300 font-semibold">
            WAV 16-bit Mono
          </span>
        </div>
      </div>

      <!-- Scrub Bar / Waveform visualization -->
      <div class="space-y-1">
        <div class="relative w-full h-7 bg-slate-900 rounded-lg overflow-hidden flex items-center px-1 cursor-pointer border border-slate-800"
             (click)="seekByClick($event)">
          
          <!-- Pseudo-waveform bars -->
          <div class="absolute inset-0 flex items-center justify-between px-2 opacity-30 pointer-events-none">
            <span *ngFor="let h of waveformHeights"
                  class="w-1 rounded-full bg-violet-400"
                  [style.height.%]="h">
            </span>
          </div>

          <!-- Progress fill overlay -->
          <div class="absolute top-0 bottom-0 left-0 bg-violet-600/30 border-r-2 border-violet-400 transition-all pointer-events-none"
               [style.width.%]="progressPercent()">
          </div>
        </div>

        <!-- HTML Range Input for Accessibility / Scrubbing -->
        <input type="range"
               min="0"
               [max]="totalDuration() > 0 ? totalDuration() : 100"
               [value]="currentTime()"
               (input)="onScrub($event)"
               class="w-full h-1 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-violet-500">
      </div>

      @if (hasError()) {
        <p class="text-[11px] text-red-400 flex items-center gap-1">
          <i class="pi pi-exclamation-circle text-[10px]"></i>
          <span>No se pudo cargar el stream de audio. Verifique credenciales y almacenamiento.</span>
        </p>
      }
    </div>
  `
})
export class VoiceAudioPlayerComponent implements OnDestroy {
  @Input({ required: true }) audioUrl!: string;
  @Input() initialDurationSeconds?: number | null;

  @ViewChild('audioElement') audioElementRef?: ElementRef<HTMLAudioElement>;

  readonly isPlaying = signal<boolean>(false);
  readonly currentTime = signal<number>(0);
  readonly totalDuration = signal<number>(0);
  readonly playbackRate = signal<number>(1.0);
  readonly hasError = signal<boolean>(false);

  // 36 pseudo-waveform height percentages for visual rhythm
  readonly waveformHeights: number[] = [
    25, 40, 65, 80, 50, 30, 70, 95, 85, 60, 45, 75,
    90, 100, 65, 40, 55, 85, 90, 70, 40, 60, 80, 50,
    30, 45, 70, 85, 60, 35, 50, 75, 90, 65, 40, 20
  ];

  get progressPercent(): () => number {
    return () => {
      const dur = this.totalDuration();
      if (dur <= 0) return 0;
      return Math.min(100, (this.currentTime() / dur) * 100);
    };
  }

  ngOnDestroy(): void {
    this.stopAudio();
  }

  togglePlay(): void {
    const audio = this.audioElementRef?.nativeElement;
    if (!audio) return;

    if (this.isPlaying()) {
      audio.pause();
      this.isPlaying.set(false);
    } else {
      audio.play().then(() => {
        this.isPlaying.set(true);
        this.hasError.set(false);
      }).catch(err => {
        console.error('Audio playback error:', err);
        this.hasError.set(true);
        this.isPlaying.set(false);
      });
    }
  }

  stopAudio(): void {
    const audio = this.audioElementRef?.nativeElement;
    if (audio) {
      audio.pause();
      audio.currentTime = 0;
      this.isPlaying.set(false);
    }
  }

  onTimeUpdate(): void {
    const audio = this.audioElementRef?.nativeElement;
    if (audio) {
      this.currentTime.set(audio.currentTime);
    }
  }

  onLoadedMetadata(): void {
    const audio = this.audioElementRef?.nativeElement;
    if (audio && audio.duration && !isNaN(audio.duration) && isFinite(audio.duration)) {
      this.totalDuration.set(audio.duration);
    } else if (this.initialDurationSeconds && this.initialDurationSeconds > 0) {
      this.totalDuration.set(this.initialDurationSeconds);
    }
  }

  onEnded(): void {
    this.isPlaying.set(false);
    this.currentTime.set(0);
  }

  onError(): void {
    this.hasError.set(true);
    this.isPlaying.set(false);
  }

  onScrub(event: Event): void {
    const target = event.target as HTMLInputElement;
    const value = parseFloat(target.value);
    const audio = this.audioElementRef?.nativeElement;
    if (audio) {
      audio.currentTime = value;
      this.currentTime.set(value);
    }
  }

  seekByClick(event: MouseEvent): void {
    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const clickX = event.clientX - rect.left;
    const ratio = Math.max(0, Math.min(1, clickX / rect.width));
    const audio = this.audioElementRef?.nativeElement;
    const targetTime = ratio * this.totalDuration();
    if (audio && this.totalDuration() > 0) {
      audio.currentTime = targetTime;
      this.currentTime.set(targetTime);
    }
  }

  cyclePlaybackRate(): void {
    const audio = this.audioElementRef?.nativeElement;
    const rates = [1.0, 1.25, 1.5, 0.8];
    const current = this.playbackRate();
    const nextIdx = (rates.indexOf(current) + 1) % rates.length;
    const nextRate = rates[nextIdx];
    this.playbackRate.set(nextRate);
    if (audio) {
      audio.playbackRate = nextRate;
    }
  }

  formatTime(seconds: number): string {
    if (isNaN(seconds) || seconds < 0) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }
}
