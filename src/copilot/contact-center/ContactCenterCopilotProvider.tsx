import { ContactCenterActions } from "./ContactCenterActions";
import { ContactCenterReadables } from "./ContactCenterReadables";
import { ContactCenterFAQActions } from "./ContactCenterFAQActions";

interface Props {
  isRtl: boolean;
  assistantOpen: boolean;
}

/**
 * Thin shell provider — mounts the three hook hosts inside <CopilotKitProvider>.
 * Renders no visible UI.
 */
export function ContactCenterCopilotProvider({ isRtl, assistantOpen }: Props) {
  return (
    <>
      <ContactCenterReadables isRtl={isRtl} assistantOpen={assistantOpen} />
      <ContactCenterActions isRtl={isRtl} />
      <ContactCenterFAQActions isRtl={isRtl} />
    </>
  );
}
