import { DestroyRef, Signal, computed, inject, signal } from '@angular/core';

// Verilen UTC zamana kalan saniyeyi saniyede bir güncelleyen sinyal. Bileşen yok olunca durur.
// Injection context içinde (constructor / field initializer) çağrılmalıdır.
export function countdown(target: () => string | null | undefined): Signal<number> {
  const now = signal(Date.now());
  const timer = setInterval(() => now.set(Date.now()), 1000);
  inject(DestroyRef).onDestroy(() => clearInterval(timer));

  return computed(() => {
    const value = target();
    if (!value) return 0;
    return Math.max(0, Math.floor((new Date(value).getTime() - now()) / 1000));
  });
}

export function formatDuration(totalSeconds: number): string {
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  const pad = (value: number) => String(value).padStart(2, '0');

  if (days > 0) return `${days} gün ${hours} sa`;
  if (hours > 0) return `${hours}:${pad(minutes)}:${pad(seconds)}`;
  return `${pad(minutes)}:${pad(seconds)}`;
}
