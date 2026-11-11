import { useBrandName } from '../model/useBrandName'

/** Renders the stored Arabic institution name as plain text. */
export function BrandName() {
  return <>{useBrandName()}</>
}
