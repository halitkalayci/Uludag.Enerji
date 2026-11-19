// Tarayıcı depolaması gizli pencerede / engellendiğinde hata fırlatabilir; uygulama yine de çalışmalı.
export function readJson<T>(storage: Storage, key: string): T | null {
  try {
    const raw = storage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    return null;
  }
}

export function writeJson(storage: Storage, key: string, value: unknown): void {
  try {
    if (value === null || value === undefined) {
      storage.removeItem(key);
    } else {
      storage.setItem(key, JSON.stringify(value));
    }
  } catch {
    /* depolama kullanılamıyor: yalnızca bellekte tut */
  }
}
