import { describe, it, expect, beforeEach } from 'vitest';
import { useLanguageStore } from '@/app/store/languageStore';
import { translations } from '@/i18n/translations';

describe('languageStore & localization', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.lang = 'ar';
    document.documentElement.dir = 'rtl';
  });

  it('initializes with default arabic language and rtl direction', () => {
    const state = useLanguageStore.getState();
    expect(state.language).toBe('ar');
    expect(state.isRTL).toBe(true);
    expect(document.documentElement.lang).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('switches to english and updates html attributes to ltr', () => {
    useLanguageStore.getState().setLanguage('en');

    expect(useLanguageStore.getState().language).toBe('en');
    expect(useLanguageStore.getState().isRTL).toBe(false);
    expect(document.documentElement.lang).toBe('en');
    expect(document.documentElement.dir).toBe('ltr');
    expect(localStorage.getItem('pharma-language')).toBe('en');
  });

  it('toggles language between arabic and english', () => {
    useLanguageStore.getState().setLanguage('ar');
    expect(useLanguageStore.getState().language).toBe('ar');

    useLanguageStore.getState().toggleLanguage();
    expect(useLanguageStore.getState().language).toBe('en');
    expect(useLanguageStore.getState().isRTL).toBe(false);
    expect(document.documentElement.dir).toBe('ltr');

    useLanguageStore.getState().toggleLanguage();
    expect(useLanguageStore.getState().language).toBe('ar');
    expect(useLanguageStore.getState().isRTL).toBe(true);
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('has consistent bilingual translation keys in dictionary', () => {
    // Verify top-level sections exist in both ar and en
    expect(Object.keys(translations.ar)).toEqual(Object.keys(translations.en));

    // Verify common keys
    expect(translations.ar.common.search).toBe('بحث');
    expect(translations.en.common.search).toBe('Search');

    // Verify nav brand
    expect(translations.ar.nav.brand).toBe('فارما فايندر');
    expect(translations.en.nav.brand).toBe('PharmaFinder');
  });
});
