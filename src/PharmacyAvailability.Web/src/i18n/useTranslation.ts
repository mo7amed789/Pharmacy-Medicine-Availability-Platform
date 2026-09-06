import { useLanguageStore } from '@/app/store/languageStore';
import { translations } from './translations';

type NestedKeyOf<ObjectType extends object> = {
  [Key in keyof ObjectType & (string | number)]: ObjectType[Key] extends object
    ? `${Key}.${NestedKeyOf<ObjectType[Key]>}`
    : `${Key}`;
}[keyof ObjectType & (string | number)];

export type TranslationKeyPath = NestedKeyOf<typeof translations.ar>;

export function useTranslation() {
  const { language, isRTL, setLanguage, toggleLanguage } = useLanguageStore();

  const t = (key: string, params?: Record<string, string | number>): string => {
    const keys = key.split('.');
    
    // Resolve current language value
    let value: any = translations[language];
    for (const k of keys) {
      if (value && typeof value === 'object' && k in value) {
        value = value[k];
      } else {
        value = undefined;
        break;
      }
    }

    // Fallback to Arabic if English is missing or vice versa
    if (value === undefined || typeof value !== 'string') {
      let fallbackValue: any = translations.ar;
      for (const k of keys) {
        if (fallbackValue && typeof fallbackValue === 'object' && k in fallbackValue) {
          fallbackValue = fallbackValue[k];
        } else {
          fallbackValue = undefined;
          break;
        }
      }
      if (typeof fallbackValue === 'string') {
        value = fallbackValue;
      } else {
        return key;
      }
    }

    // Interpolation replacement: {{variable}}
    if (params) {
      let result = value as string;
      for (const [pKey, pVal] of Object.entries(params)) {
        result = result.replace(new RegExp(`{{\\s*${pKey}\\s*}}`, 'g'), String(pVal));
      }
      return result;
    }

    return value as string;
  };

  return {
    t,
    language,
    isRTL,
    setLanguage,
    toggleLanguage,
  };
}
