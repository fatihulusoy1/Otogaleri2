import { useEffect, useMemo, useRef, useState } from "react";
import type { LookupOption } from "../types";

interface SearchableSelectProps {
  label: string;
  value: string;
  options: LookupOption[];
  placeholder: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  required?: boolean;
}

export function SearchableSelect({
  label,
  value,
  options,
  placeholder,
  onChange,
  disabled = false,
  required = false
}: SearchableSelectProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");

  const selectedOption = options.find((option) => option.id === value) ?? null;

  const filteredOptions = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
      return options;
    }

    return options.filter((option) => option.name.toLowerCase().includes(normalizedQuery));
  }, [options, query]);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  useEffect(() => {
    if (!open) {
      setQuery("");
    }
  }, [open]);

  return (
    <label>
      <span className={required ? "field-label required" : "field-label"}>{label}</span>
      <div className={`searchable-select ${disabled ? "disabled" : ""}`} ref={containerRef}>
        <button
          type="button"
          className="searchable-trigger"
          onClick={() => !disabled && setOpen((current) => !current)}
          disabled={disabled}
        >
          <span>{selectedOption?.name ?? placeholder}</span>
          <strong>Ara</strong>
        </button>

        {open ? (
          <div className="searchable-menu">
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={`${label} ara`}
              autoFocus
            />
            <div className="searchable-options">
              {filteredOptions.map((option) => (
                <button
                  type="button"
                  key={option.id}
                  className={`searchable-option ${option.id === value ? "active" : ""}`}
                  onClick={() => {
                    onChange(option.id);
                    setOpen(false);
                  }}
                >
                  {option.name}
                </button>
              ))}
              {filteredOptions.length === 0 ? (
                <div className="searchable-empty">Sonuc bulunamadi.</div>
              ) : null}
            </div>
          </div>
        ) : null}
      </div>
    </label>
  );
}
