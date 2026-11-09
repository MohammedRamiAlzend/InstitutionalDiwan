/**
 * Parts of the product that are built but switched off for now. Turning one on again needs only
 * its flag here: the code, the API and the stored data all stay in place.
 */
export const FEATURES: {
  readonly externalOrganizations: boolean
  readonly personParticipants: boolean
  readonly participants: boolean
} = {
  /** The external organizations tab, and choosing an organization as a participant or field source. */
  externalOrganizations: false,
  /** Adding a participant by a person's name. With this and the one above off, every participant is a department. */
  personParticipants: false,
  /** A document's participants tab and its add-participant action. */
  participants: false,
}
