import { useTranslation } from 'react-i18next';

export function LanguageToggle() {
  const { i18n } = useTranslation();
  return (
    <select aria-label="Language / Dil" title="Language / Dil" value={i18n.language}
      onChange={(event) => { void i18n.changeLanguage(event.target.value); }}
      className="h-8 border border-border bg-surface px-1 text-xs font-semibold text-foreground outline-none focus:border-primary">
      <option value="tr">TR</option>
      <option value="en">EN</option>
    </select>
  );
}
