export type Application = {
  referenceCode: string;
  status: number;
  fullNameBn: string | null;
  fullNameEn: string | null;
  contactNumber: string | null;
  createdAtUtc: string;
  submittedAtUtc: string | null;
  codeOfConductAccepted: boolean;
  [key: string]: string | number | boolean | null;
};
export type Payment = {
  id: string;
  type: number;
  amount: number;
  method: number;
  transactionReference: string | null;
  note: string | null;
  recordedAtUtc: string;
};
export type Detail = {
  application: Application;
  member: null | {
    id: string;
    membershipNumber: string | null;
    status: number;
    contributions: Payment[];
  };
  verifiedBy: string | null;
  verifiedAtUtc: string | null;
  approvedBy: string | null;
  approvedAtUtc: string | null;
  rejectedBy: string | null;
  rejectedAtUtc: string | null;
  rejectionReason: string | null;
  entryChannel: number | null;
  enteredByStaffId: string | null;
};
export type Queue = {
  items: Application[];
  totalCount: number;
  page: number;
  pageSize: number;
};
export const statuses = [
  "Draft",
  "Submitted",
  "Verified",
  "Approved",
  "Active",
  "Rejected",
];
export const paymentTypes = [
  "Membership fee",
  "Regular dues",
  "Event contribution",
  "Donation",
];
export const textFields: [string, string][] = [
  ["email", "Email"],
  ["otherHelpBn", "Other help (Bangla)"],
  ["otherHelpEn", "Other help (English)"],
  ["aboutSelfBn", "About yourself (Bangla)"],
  ["aboutSelfEn", "About yourself (English)"],
  ["signatureName", "Signature name"],
  ["applicationDate", "Application date"],
  ["consentVersion", "Consent version"],
  ["consentAcceptedAtUtc", "Consent recorded at"],
  ["fullNameBn", "Full name (Bangla)"],
  ["fullNameEn", "Full name (English)"],
  ["contactNumber", "Contact number"],
  ["fatherNameBn", "Father’s name (Bangla)"],
  ["fatherNameEn", "Father’s name (English)"],
  ["motherNameBn", "Mother’s name (Bangla)"],
  ["motherNameEn", "Mother’s name (English)"],
  ["nidNumber", "NID number"],
  ["permanentAddressBn", "Permanent address (Bangla)"],
  ["permanentAddressEn", "Permanent address (English)"],
  ["temporaryAddressBn", "Current address (Bangla)"],
  ["temporaryAddressEn", "Current address (English)"],
  ["occupationBn", "Occupation (Bangla)"],
  ["occupationEn", "Occupation (English)"],
  ["educationBn", "Education (Bangla)"],
  ["educationEn", "Education (English)"],
  ["emergencyContactNumber", "Emergency contact"],
  ["bloodGroup", "Blood group"],
  ["purposeOfJoiningBn", "Reason for joining (Bangla)"],
  ["purposeOfJoiningEn", "Reason for joining (English)"],
  ["lifeGoalBn", "Life goal (Bangla)"],
  ["lifeGoalEn", "Life goal (English)"],
  ["specialSkillsBn", "Skills (Bangla)"],
  ["specialSkillsEn", "Skills (English)"],
  ["currentChallengeBn", "Current challenges (Bangla)"],
  ["currentChallengeEn", "Current challenges (English)"],
];
export const enumFields: [string, string, string[]][] = [
  ["gender", "Gender", ["Male", "Female"]],
  [
    "maritalStatus",
    "Marital status",
    ["Unmarried", "Married", "Divorced", "Widowed", "Separated"],
  ],
  [
    "timeCommitment",
    "Time per month",
    ["Unspecified", "Two hours", "Five hours", "Ten hours"],
  ],
  [
    "contributionIntent",
    "Financial contribution intent",
    ["Unspecified", "Yes", "No", "Decide later"],
  ],
];
export const helpOptions = [
  "Education",
  "Business",
  "Technology",
  "Social work",
  "Medical",
  "Culture",
  "Other",
];
