export function AddressDisplay({
  addressLine1,
  addressLine2,
  barangay,
  barangayLabel,
  city,
  administrativeArea,
  postalCode,
  country,
}: {
  addressLine1?: string | null;
  addressLine2?: string | null;
  barangay?: string | null;
  barangayLabel?: string;
  city?: string | null;
  administrativeArea?: string | null;
  postalCode?: string | null;
  country?: string | null;
}) {
  const place = [city, administrativeArea].filter(Boolean).join(", ");
  return (
    <div className="flex flex-col gap-1">
      {addressLine1 ? <p className="m-0">{addressLine1}</p> : null}
      {addressLine2 ? <p className="m-0">{addressLine2}</p> : null}
      {barangay ? <p className="m-0">{barangayLabel ? `${barangayLabel} ${barangay}` : barangay}</p> : null}
      {place || postalCode ? <p className="m-0">{[place, postalCode].filter(Boolean).join(" ")}</p> : null}
      {country ? <p className="m-0">{country}</p> : null}
    </div>
  );
}
