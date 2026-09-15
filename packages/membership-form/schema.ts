export type Language = "bn" | "en";
export type Field = {
  key: string;
  bn: string;
  en: string;
  section: number;
  type?: "text" | "textarea" | "email" | "number" | "date" | "select";
  required?: boolean;
  max?: number;
  min?: number;
  options?: [number | string, string, string][];
  otherOnly?: boolean;
};
export const consentVersion = "sadria-membership-2026-09-v1";
export const conduct = {
  bn: "১. আমি সোসাইটির সকল নিয়ম-শৃঙ্খলা মেনে চলব।\n২. সাদরিয়া সমাজের পারস্পরিক ভ্রাতৃত্ব এবং ঐক্য বজায় রাখব।",
  en: "1. I will follow the Society's rules and discipline.\n2. I will uphold mutual fellowship and unity in the Sadria community.",
};
export const declaration = {
  bn: "আমি ঘোষনা করছি যে এই ফরমে প্রদত্ত সকল তথ্য সত্য এবং আমি সোসাইটির কল্যাণে সচেষ্ট থাকব।",
  en: "I declare that the information in this form is true and that I will work for the welfare of the Society.",
};
export const oath = {
  bn: "আমি শপথ করছি যে, সাদরিয়া সোসাইটির আদর্শ, বিধিবিধান ও আচরণবিধি মেনে সততা, আত্মশুদ্ধি, ভ্রাতৃত্ব, মানবসেবা ও ঐক্যের চেতনায় একজন আদর্শ সদস্য হিসেবে নিজেকে গড়ে তুলবো এবং সোসাইটির মর্যাদা ও কল্যাণে সর্বদা আন্তরিকভাবে কাজ করবো।",
  en: "I pledge to follow the ideals, rules and code of conduct of Sadria Society; to develop myself through honesty, self-purification, fellowship, service to humanity and unity; and to work sincerely for the Society's dignity and welfare.",
};
export const sections = [
  ["ব্যক্তিগত তথ্য", "Basic information"],
  ["উদ্দেশ্য ও লক্ষ্য", "Purpose and goals"],
  ["দক্ষতা ও অবদান", "Skills and contributions"],
  ["বর্তমান চ্যালেঞ্জ", "Current challenges"],
  ["পরিচিতি ও স্বাক্ষর", "About you and signature"],
  ["পর্যালোচনা ও সম্মতি", "Review and consent"],
];
const f = (
  key: string,
  bn: string,
  en: string,
  section: number,
  extras: Partial<Field> = {},
): Field => ({ key, bn, en, section, required: true, ...extras });
const optional = { required: false };
export const fields: Field[] = [
  f("fullNameBn", "পূর্ণ নাম (বাংলা)", "Full name (Bangla)", 0),
  f("fullNameEn", "পূর্ণ নাম (ইংরেজি)", "Full name (English)", 0),
  f("email", "ইমেইল", "Email", 0, { type: "email", max: 254 }),
  f("fatherNameBn", "পিতার নাম", "Father’s name (Bangla)", 0),
  f(
    "fatherNameEn",
    "পিতার নাম (ইংরেজি)",
    "Father’s name (English)",
    0,
    optional,
  ),
  f("motherNameBn", "মাতার নাম", "Mother’s name (Bangla)", 0),
  f(
    "motherNameEn",
    "মাতার নাম (ইংরেজি)",
    "Mother’s name (English)",
    0,
    optional,
  ),
  f(
    "contactNumber",
    "যোগাযোগ নম্বর (ইংরেজি অঙ্ক)",
    "Contact number (English digits)",
    0,
    { max: 20 },
  ),
  f("nidNumber", "জাতীয় পরিচয়পত্র নম্বর", "NID number", 0, { max: 30 }),
  f("permanentAddressBn", "স্থায়ী ঠিকানা", "Permanent address (Bangla)", 0, {
    type: "textarea",
    max: 1000,
  }),
  f(
    "permanentAddressEn",
    "স্থায়ী ঠিকানা (ইংরেজি)",
    "Permanent address (English)",
    0,
    { ...optional, type: "textarea", max: 1000 },
  ),
  f("temporaryAddressBn", "অস্থায়ী ঠিকানা", "Current address (Bangla)", 0, {
    type: "textarea",
    max: 1000,
  }),
  f(
    "temporaryAddressEn",
    "অস্থায়ী ঠিকানা (ইংরেজি)",
    "Current address (English)",
    0,
    { ...optional, type: "textarea", max: 1000 },
  ),
  f("maritalStatus", "বৈবাহিক অবস্থা", "Marital status", 0, {
    type: "select",
    options: [
      [0, "অবিবাহিত", "Unmarried"],
      [1, "বিবাহিত", "Married"],
      [2, "তালাকপ্রাপ্ত", "Divorced"],
      [3, "বিপত্নীক", "Widowed"],
      [4, "স্বত্ববিচ্ছিন্ন", "Separated"],
    ],
  }),
  f("age", "বয়স", "Age", 0, { type: "number", min: 0, max: 130 }),
  f("occupationBn", "পেশা", "Occupation (Bangla)", 0),
  f("occupationEn", "পেশা (ইংরেজি)", "Occupation (English)", 0, optional),
  f("bloodGroup", "রক্তের গ্রুপ", "Blood group", 0, {
    type: "select",
    options: ["A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-"].map((v) => [
      v,
      v,
      v,
    ]),
  }),
  f("gender", "লিঙ্গ", "Gender", 0, {
    type: "select",
    options: [
      [0, "পুরুষ", "Male"],
      [1, "মহিলা", "Female"],
    ],
  }),
  f("educationBn", "শিক্ষাগত যোগ্যতা", "Education (Bangla)", 0),
  f(
    "educationEn",
    "শিক্ষাগত যোগ্যতা (ইংরেজি)",
    "Education (English)",
    0,
    optional,
  ),
  f(
    "emergencyContactNumber",
    "জরুরি যোগাযোগ নম্বর",
    "Emergency contact number",
    0,
    { max: 20 },
  ),
  f(
    "purposeOfJoiningBn",
    "সোসাইটিতে যুক্ত হওয়ার মূল উদ্দেশ্য",
    "Reason for joining (Bangla)",
    1,
    { type: "textarea", max: 2000 },
  ),
  f(
    "purposeOfJoiningEn",
    "যুক্ত হওয়ার উদ্দেশ্য (ইংরেজি)",
    "Reason for joining (English)",
    1,
    { ...optional, type: "textarea", max: 2000 },
  ),
  f("lifeGoalBn", "আপনার জীবনের প্রধান লক্ষ্য", "Main life goal (Bangla)", 1, {
    type: "textarea",
    max: 2000,
  }),
  f("lifeGoalEn", "জীবনের লক্ষ্য (ইংরেজি)", "Main life goal (English)", 1, {
    ...optional,
    type: "textarea",
    max: 2000,
  }),
  f("specialSkillsBn", "আপনার বিশেষ দক্ষতা", "Special skills (Bangla)", 2, {
    type: "textarea",
    max: 2000,
  }),
  f("specialSkillsEn", "বিশেষ দক্ষতা (ইংরেজি)", "Special skills (English)", 2, {
    ...optional,
    type: "textarea",
    max: 2000,
  }),
  f(
    "otherHelpBn",
    "অন্যান্য সহযোগিতার বিবরণ",
    "Other area of help (Bangla)",
    2,
    { max: 500, otherOnly: true },
  ),
  f(
    "otherHelpEn",
    "অন্যান্য সহযোগিতা (ইংরেজি)",
    "Other area of help (English)",
    2,
    { ...optional, max: 500, otherOnly: true },
  ),
  f("timeCommitment", "মাসিক সময়", "Time per month", 2, {
    type: "select",
    options: [
      [1, "২ ঘণ্টা", "2 hours"],
      [2, "৫ ঘণ্টা", "5 hours"],
      [3, "১০ ঘণ্টা", "10 hours"],
    ],
  }),
  f(
    "contributionIntent",
    "আর্থিক অবদান",
    "Financial contribution intention",
    2,
    {
      type: "select",
      options: [
        [1, "হ্যাঁ", "Yes"],
        [2, "না", "No"],
        [3, "পরে জানাব", "Decide later"],
      ],
    },
  ),
  f(
    "currentChallengeBn",
    "জীবনের প্রধান সমস্যা/চ্যালেঞ্জ ও প্রত্যাশিত সাহায্য",
    "Main challenges and expected help (Bangla)",
    3,
    { type: "textarea", max: 2000 },
  ),
  f(
    "currentChallengeEn",
    "চ্যালেঞ্জ ও প্রত্যাশিত সাহায্য (ইংরেজি)",
    "Challenges and expected help (English)",
    3,
    { ...optional, type: "textarea", max: 2000 },
  ),
  f(
    "committedSinceYear",
    "আদর্শের সাথে সম্পৃক্ততার সাল",
    "Year you became committed to the ideals",
    4,
    { type: "number", min: 1900, max: new Date().getUTCFullYear() },
  ),
  f(
    "aboutSelfBn",
    "নিজের সম্পর্কে সংক্ষেপে লিখুন",
    "About yourself (Bangla)",
    4,
    { type: "textarea", max: 2000 },
  ),
  f("aboutSelfEn", "নিজের সম্পর্কে (ইংরেজি)", "About yourself (English)", 4, {
    ...optional,
    type: "textarea",
    max: 2000,
  }),
  f(
    "signatureName",
    "স্বাক্ষর হিসেবে নিজের পূর্ণ নাম লিখুন",
    "Signature: type your full name",
    4,
  ),
  f("applicationDate", "আবেদনের তারিখ", "Application date", 4, {
    type: "date",
  }),
];
export const helpOptions = [
  [1, "শিক্ষা", "Education"],
  [2, "ব্যবসা", "Business"],
  [4, "প্রযুক্তি", "Technology"],
  [8, "সামাজিক কাজ", "Social work"],
  [16, "চিকিৎসা", "Medical"],
  [32, "সংস্কৃতি", "Culture"],
  [64, "অন্যান্য", "Other"],
] as const;
export type Values = Record<string, string | number | null | undefined>;
export function validate(values: Values, section?: number): string[] {
  const errors: string[] = [];
  for (const field of fields.filter(
    (f) => section === undefined || f.section === section,
  )) {
    if (field.otherOnly && !(Number(values.helpCategories || 0) & 64)) continue;
    const v = values[field.key],
      empty = v === null || v === undefined || String(v).trim() === "";
    if (empty) {
      if (field.required) errors.push(field.key);
      continue;
    }
    if (
      field.type === "number" &&
      (!Number.isInteger(Number(v)) ||
        Number(v) < (field.min ?? 0) ||
        Number(v) > (field.max ?? Infinity))
    )
      errors.push(field.key);
    if (
      field.type === "select" &&
      !field.options?.some((o) => String(o[0]) === String(v))
    )
      errors.push(field.key);
    if (
      field.type !== "number" &&
      field.type !== "select" &&
      String(v).length > (field.max ?? 200)
    )
      errors.push(field.key);
    if (field.type === "email" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v)))
      errors.push(field.key);
    if (
      ["contactNumber", "emergencyContactNumber"].includes(field.key) &&
      !/^\+?[0-9]{7,15}$/.test(String(v))
    )
      errors.push(field.key);
    if (field.key === "nidNumber" && !/^[0-9]{1,30}$/.test(String(v)))
      errors.push(field.key);
    if (
      field.type === "date" &&
      (Number.isNaN(Date.parse(String(v))) ||
        new Date(String(v)).toISOString().slice(0, 10) !== String(v) ||
        !/^\d{4}-\d{2}-\d{2}$/.test(String(v)) ||
        String(v) >
          new Date(Date.now() + 6 * 3600000).toISOString().slice(0, 10))
    )
      errors.push(field.key);
  }
  if (
    (section === undefined || section === 2) &&
    (!Number(values.helpCategories) ||
      (Number(values.helpCategories) & ~127) !== 0)
  )
    errors.push("helpCategories");
  return [...new Set(errors)];
}
