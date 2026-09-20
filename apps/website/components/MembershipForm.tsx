"use client";
import { useEffect, useRef, useState } from "react";
import {
  fields,
  helpOptions,
  sections,
  validate,
  consentVersion,
  type Language,
  type Values,
} from "../../../packages/membership-form/schema";
type Credentials = {
  referenceCode: string;
  contactNumber: string;
  resumeToken: string;
};
type RecordData = Values & {
  status: number;
  photoUrl?: string;
  submittedAtUtc?: string;
};
type Definition = {
  version: string;
  conductBn: string;
  conductEn: string;
  declarationBn: string;
  declarationEn: string;
  oathBn: string;
  oathEn: string;
};
async function request<T>(
  path: string,
  method = "GET",
  body?: unknown,
): Promise<T> {
  const response = await fetch("/api/v1/membership/" + path, {
    method,
    cache: "no-store",
    headers:
      body instanceof FormData
        ? undefined
        : { "Content-Type": "application/json" },
    body:
      body === undefined
        ? undefined
        : body instanceof FormData
          ? body
          : JSON.stringify(body),
  });
  const result = await response.json().catch(() => null);
  if (!response.ok || result?.success === false)
    throw new Error(
      response.status === 429
        ? "Too many requests. Please wait a minute and try again."
        : result?.message ||
            "The request could not be completed. Your entries remain on this screen.",
    );
  return result.data;
}
function download(name: string, data: unknown) {
  const url = URL.createObjectURL(
    new Blob([JSON.stringify(data, null, 2)], { type: "application/json" }),
  );
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
export default function MembershipForm({ language }: { language: Language }) {
  const [lang, setLang] = useState(language),
    [mode, setMode] = useState<"start" | "resume" | "credentials" | "form">(
      "start",
    );
  const [values, setValues] = useState<Values>({}),
    [credentials, setCredentials] = useState<Credentials>({
      referenceCode: "",
      contactNumber: "",
      resumeToken: "",
    });
  const [step, setStep] = useState(0),
    [status, setStatus] = useState(0),
    [savedKey, setSavedKey] = useState(false),
    [photo, setPhoto] = useState(false),
    [preview, setPreview] = useState("");
  const [consents, setConsents] = useState([false, false, false]),
    [definition, setDefinition] = useState<Definition>(),
    [error, setError] = useState(""),
    [notice, setNotice] = useState(""),
    [invalid, setInvalid] = useState<string[]>([]),
    [busy, setBusy] = useState(false),
    [dirty, setDirty] = useState(false);
  const running = useRef(false);
  const t = (bn: string, en: string) => (lang === "bn" ? bn : en);
  useEffect(() => {
    request<Definition>("form-definition")
      .then(setDefinition)
      .catch(() =>
        setError(
          "Could not load the form terms. Reload this page before submitting.",
        ),
      );
  }, []);
  useEffect(() => {
    document.documentElement.lang = lang;
  }, [lang]);
  useEffect(
    () => () => {
      if (preview) URL.revokeObjectURL(preview);
    },
    [preview],
  );
  useEffect(() => {
    const warn = (e: BeforeUnloadEvent) => {
      if (dirty) {
        e.preventDefault();
        e.returnValue = "";
      }
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);
  const change = (key: string, value: string | number) => {
    setValues((v) => ({ ...v, [key]: value }));
    setDirty(true);
    setNotice("");
  };
  async function run(action: () => Promise<void>) {
    if (running.current) return;
    running.current = true;
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await action();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Request failed.");
    } finally {
      running.current = false;
      setBusy(false);
    }
  }
  function absorb(data: RecordData) {
    setValues(data);
    setStatus(data.status);
    setPhoto(data.photoUrl === "uploaded");
    setDirty(false);
  }
  async function save() {
    const payload: Values = {};
    for (const f of fields)
      if (
        values[f.key] !== null &&
        values[f.key] !== undefined &&
        !(
          values[f.key] === "" &&
          ["number", "date", "select"].includes(f.type || "")
        )
      )
        payload[f.key] = values[f.key];
    payload.helpCategories = Number(values.helpCategories || 0);
    const data = await request<RecordData>(
      "applications/" + credentials.referenceCode,
      "PATCH",
      { ...payload, ...credentials },
    );
    absorb(data);
    return data;
  }
  function check(section?: number) {
    const errors = validate(values, section);
    if ((section === undefined || section === 4) && !photo)
      errors.push("photo");
    setInvalid(errors);
    if (errors.length) {
      setError(
        t(
          "চিহ্নিত ঘরগুলো পূরণ বা সংশোধন করুন।",
          "Complete or correct the marked fields.",
        ),
      );
      if (section === undefined)
        setStep(
          fields.find((f) => f.key === errors[0])?.section ??
            (errors[0] === "helpCategories" ? 2 : 4),
        );
      return false;
    }
    return true;
  }
  const label = (key: string) =>
    fields.find((f) => f.key === key)?.[lang] || key;
  async function resume() {
    const data = await request<RecordData>(
      "applications/resume",
      "POST",
      credentials,
    );
    absorb(data);
    setMode("form");
    setStep(0);
    setPreview("");
  }
  function input(key: string, readOnly = false) {
    const f = fields.find((f) => f.key === key)!;
    const common = {
      id: key,
      name: key,
      value: values[key] ?? "",
      required: !!f.required,
      "aria-invalid": invalid.includes(key),
      onChange: (
        e: React.ChangeEvent<
          HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >,
      ) =>
        change(
          key,
          (f.type === "number" || typeof f.options?.[0]?.[0] === "number") &&
            e.target.value !== ""
            ? Number(e.target.value)
            : e.target.value,
        ),
    };
    return (
      <label className="field" key={key} htmlFor={key}>
        <span>
          {f[lang]} {f.required ? "*" : t("(ঐচ্ছিক)", "(optional)")}
        </span>
        {f.type === "textarea" ? (
          <textarea {...common} maxLength={f.max ?? 200} />
        ) : f.type === "select" ? (
          <select {...common}>
            <option value="">{t("নির্বাচন করুন", "Select")}</option>
            {f.options?.map((o) => (
              <option key={o[0]} value={o[0]}>
                {o[lang === "bn" ? 1 : 2]}
              </option>
            ))}
          </select>
        ) : (
          <input
            {...common}
            type={f.type || "text"}
            readOnly={readOnly}
            min={f.min}
            max={f.type === "number" ? f.max : undefined}
            maxLength={f.type === "number" ? undefined : (f.max ?? 200)}
            autoComplete={
              key === "email"
                ? "email"
                : key === "contactNumber"
                  ? "tel"
                  : "off"
            }
          />
        )}
      </label>
    );
  }
  const statusNames =
    lang === "bn"
      ? [
          "খসড়া",
          "জমা হয়েছে",
          "যাচাই হয়েছে",
          "অনুমোদিত",
          "সক্রিয়",
          "প্রত্যাখ্যাত",
        ]
      : ["Draft", "Submitted", "Verified", "Approved", "Active", "Rejected"];
  return (
    <>
      <header className="max-w-6xl mx-auto px-5 py-6 flex justify-between items-center gap-4">
        <div>
          <strong>
            {t("সাদরিয়া সোসাইটি ইন্টাঃ", "Sadria Society International")}
          </strong>
          <p className="text-sm m-0">
            {t(
              "আত্মদর্শনের পথে মানবতার কল্যাণে",
              "On the path of self-reflection, for humanity",
            )}
          </p>
        </div>
        <button
          className="secondary"
          onClick={() => setLang(lang === "bn" ? "en" : "bn")}
          lang={lang === "bn" ? "en" : "bn"}
        >
          {lang === "bn" ? "English" : "বাংলা"}
        </button>
      </header>
      <div className="max-w-6xl mx-auto px-5 pb-16">
        <div className="py-7">
          <p className="text-sm uppercase tracking-widest">
            {t("সদস্যপদ", "Membership")}
          </p>
          <h1 className="text-3xl md:text-4xl font-semibold">
            {t(
              "সদস্য নিবন্ধন ও উন্নয়ন ফর্ম",
              "Member registration and development",
            )}
          </h1>
          <p>
            {t(
              "সময় নিয়ে পূরণ করুন। খসড়া সংরক্ষণ করে পরে ফিরে আসতে পারবেন।",
              "Take your time. Save a draft and return when you are ready.",
            )}
          </p>
        </div>
        {error && (
          <div className="notice error mb-5" role="alert">
            {error}
          </div>
        )}
        {notice && (
          <div className="notice mb-5" role="status">
            {notice}
          </div>
        )}
        <fieldset disabled={busy} className="border-0 p-0 m-0 min-w-0">
          {(mode === "start" || mode === "resume") && (
            <div className="card max-w-2xl">
              <div className="flex gap-3 mb-6">
                <button
                  className={mode === "start" ? "primary" : "secondary"}
                  onClick={() => {
                    setMode("start");
                    setError("");
                  }}
                >
                  {t("নতুন আবেদন", "New application")}
                </button>
                <button
                  className={mode === "resume" ? "primary" : "secondary"}
                  onClick={() => {
                    setMode("resume");
                    setError("");
                  }}
                >
                  {t("আগের আবেদন খুলুন", "Resume application")}
                </button>
              </div>
              <form
                className="grid gap-5"
                onSubmit={(e) => {
                  e.preventDefault();
                  void run(async () => {
                    if (mode === "resume") {
                      await resume();
                      return;
                    }
                    const data = await request<{
                      referenceCode: string;
                      resumeToken: string;
                    }>("applications", "POST", {
                      fullNameBn: values.fullNameBn,
                      fullNameEn: values.fullNameEn,
                      contactNumber: values.contactNumber,
                    });
                    setCredentials({
                      ...data,
                      contactNumber: String(values.contactNumber),
                    });
                    setDirty(false);
                    setMode("credentials");
                  });
                }}
              >
                {mode === "start" ? (
                  <>
                    {input("fullNameBn")}
                    {input("fullNameEn")}
                    {input("contactNumber")}
                    <p className="text-sm">
                      {t(
                        "আপনার তথ্য শুধু সদস্যপদ প্রক্রিয়ায় দায়িত্বপ্রাপ্ত কর্মীরা দেখবেন। কোনো গুগল অ্যাকাউন্ট বা OTP প্রয়োজন নেই।",
                        "Your application is available to staff responsible for membership. No Google account or OTP is required.",
                      )}
                    </p>
                  </>
                ) : (
                  <>
                    {(
                      ["referenceCode", "contactNumber", "resumeToken"] as const
                    ).map((key, i) => (
                      <label className="field" key={key}>
                        <span>
                          {
                            [
                              t("রেফারেন্স কোড", "Reference code"),
                              t("যোগাযোগ নম্বর", "Contact number"),
                              t(
                                "ব্যক্তিগত পুনরায় প্রবেশের টোকেন",
                                "Private resume token",
                              ),
                            ][i]
                          }
                        </span>
                        <input
                          required
                          value={credentials[key]}
                          type={key === "resumeToken" ? "password" : "text"}
                          autoComplete="off"
                          onChange={(e) =>
                            setCredentials((c) => ({
                              ...c,
                              [key]: e.target.value.trim(),
                            }))
                          }
                        />
                      </label>
                    ))}
                    <p>
                      {t(
                        "টোকেনটি গোপন রাখুন। এটি SMS-এ পাঠানো হয় না।",
                        "Keep the token private. It is not sent by SMS.",
                      )}
                    </p>
                  </>
                )}
                <button className="primary" type="submit">
                  {busy
                    ? t("অপেক্ষা করুন…", "Please wait…")
                    : mode === "start"
                      ? t("আবেদন শুরু করুন", "Start application")
                      : t("আবেদন খুলুন", "Open application")}
                </button>
              </form>
            </div>
          )}
          {mode === "credentials" && (
            <section className="card max-w-2xl grid gap-5">
              <h2 className="text-2xl">
                {t("ফিরে আসার তথ্য সংরক্ষণ করুন", "Save your recovery details")}
              </h2>
              <p>
                {t(
                  "পৃষ্ঠাটি বন্ধ করার আগে এই তথ্য সংরক্ষণ করুন। টোকেন ছাড়া অনলাইনে আবেদন খোলা যাবে না।",
                  "Save these details before closing this page. You need the private token to reopen your application online.",
                )}
              </p>
              <p>
                {t("রেফারেন্স", "Reference")}:{" "}
                <strong>{credentials.referenceCode}</strong>
              </p>
              <label className="field">
                <span>{t("ব্যক্তিগত টোকেন", "Private token")}</span>
                <input readOnly value={credentials.resumeToken} />
              </label>
              <button
                className="secondary"
                onClick={() =>
                  download(
                    "membership-recovery-" +
                      credentials.referenceCode +
                      ".json",
                    credentials,
                  )
                }
              >
                {t("পুনঃপ্রবেশের তথ্য ডাউনলোড", "Download recovery details")}
              </button>
              <label className="consent">
                <input
                  type="checkbox"
                  checked={savedKey}
                  onChange={(e) => setSavedKey(e.target.checked)}
                />
                {t(
                  "আমি তথ্যগুলো নিরাপদে সংরক্ষণ করেছি।",
                  "I have saved these details securely.",
                )}
              </label>
              <button
                className="primary"
                disabled={!savedKey}
                onClick={() => setMode("form")}
              >
                {t("ফর্ম পূরণ করুন", "Continue to form")}
              </button>
            </section>
          )}
          {mode === "form" && status !== 0 && (
            <section className="card max-w-2xl">
              <h2 className="text-2xl">
                {statusNames[status] || "Application status"}
              </h2>
              <p>
                {t("রেফারেন্স", "Reference")}:{" "}
                <strong>{credentials.referenceCode}</strong>
              </p>
              <p>
                {t("জমার তারিখ", "Submitted")}:{" "}
                {String(values.submittedAtUtc || "—")}
              </p>
              <p>
                {t(
                  "যাচাই ও অনুমোদন আলাদা ধাপে সম্পন্ন হবে। আনুষ্ঠানিক সদস্যপদের তারিখ প্রশাসক নির্ধারণ করবেন।",
                  "Verification and approval are separate steps. Staff will record the formal membership date.",
                )}
              </p>
              <div className="flex gap-3 flex-wrap">
                <button className="secondary" onClick={() => void run(resume)}>
                  {t("অবস্থা দেখুন", "Refresh status")}
                </button>
                <button
                  className="secondary"
                  onClick={() =>
                    download(
                      "membership-acknowledgement-" +
                        credentials.referenceCode +
                        ".json",
                      {
                        referenceCode: credentials.referenceCode,
                        status: statusNames[status],
                        submittedAtUtc: values.submittedAtUtc,
                      },
                    )
                  }
                >
                  {t("প্রাপ্তিস্বীকার ডাউনলোড", "Download acknowledgement")}
                </button>
              </div>
            </section>
          )}
          {mode === "form" && status === 0 && (
            <div className="grid md:grid-cols-[210px_1fr] gap-6">
              <nav
                aria-label={t("আবেদনের ধাপ", "Application steps")}
                className="flex md:flex-col gap-2 flex-wrap"
              >
                {sections.map((s, i) => (
                  <button
                    key={i}
                    className="step"
                    aria-current={step === i ? "step" : undefined}
                    onClick={() => {
                      setStep(i);
                      setError("");
                    }}
                  >
                    {i + 1}. {s[lang === "bn" ? 0 : 1]}
                  </button>
                ))}
              </nav>
              <section className="card min-w-0">
                <div className="flex justify-between gap-3 mb-6 flex-wrap">
                  <h2 className="text-2xl font-semibold m-0">
                    {sections[step][lang === "bn" ? 0 : 1]}
                  </h2>
                  <span className="text-sm">{credentials.referenceCode}</span>
                </div>
                {step < 5 ? (
                  <div className="grid sm:grid-cols-2 gap-5">
                    {fields
                      .filter(
                        (f) =>
                          f.section === step &&
                          (!f.otherOnly ||
                            !!(Number(values.helpCategories || 0) & 64)),
                      )
                      .map((f) => input(f.key, f.key === "contactNumber"))}
                    {step === 2 && (
                      <div className="sm:col-span-2">
                        <p>
                          {t(
                            "কোন ক্ষেত্রে সাহায্য করতে পারবেন? *",
                            "Areas in which you can help *",
                          )}
                        </p>
                        <div className="flex flex-wrap gap-4">
                          {helpOptions.map(([bit, bn, en]) => (
                            <label key={bit}>
                              <input
                                type="checkbox"
                                checked={
                                  !!(Number(values.helpCategories || 0) & bit)
                                }
                                onChange={(e) =>
                                  change(
                                    "helpCategories",
                                    e.target.checked
                                      ? Number(values.helpCategories || 0) | bit
                                      : Number(values.helpCategories || 0) &
                                          ~bit,
                                  )
                                }
                              />{" "}
                              {t(bn, en)}
                            </label>
                          ))}
                        </div>
                        {invalid.includes("helpCategories") && (
                          <p role="alert">
                            {t(
                              "অন্তত একটি নির্বাচন করুন।",
                              "Choose at least one area.",
                            )}
                          </p>
                        )}
                        <p className="text-sm">
                          {t(
                            "আর্থিক অবদানের ইচ্ছা জানানো অর্থ পরিশোধ নয়।",
                            "Your contribution intention does not record a payment.",
                          )}
                        </p>
                      </div>
                    )}
                    {step === 4 && (
                      <div className="sm:col-span-2 grid gap-4">
                        <label className="field">
                          <span>
                            {t(
                              "আপনার ছবি * (JPEG/PNG, সর্বোচ্চ ১০ MB)",
                              "Your photo * (JPEG/PNG, up to 10 MB)",
                            )}
                          </span>
                          <input
                            type="file"
                            accept="image/jpeg,image/png"
                            aria-invalid={invalid.includes("photo")}
                            onChange={(e) => {
                              const file = e.target.files?.[0];
                              if (!file) return;
                              void run(async () => {
                                if (
                                  file.size > 10 * 1024 * 1024 ||
                                  !["image/jpeg", "image/png"].includes(
                                    file.type,
                                  )
                                )
                                  throw new Error(
                                    "Choose a JPEG or PNG image up to 10 MB.",
                                  );
                                const form = new FormData();
                                form.append(
                                  "contactNumber",
                                  credentials.contactNumber,
                                );
                                form.append(
                                  "resumeToken",
                                  credentials.resumeToken,
                                );
                                form.append("photo", file);
                                await request(
                                  "applications/" +
                                    credentials.referenceCode +
                                    "/photo",
                                  "POST",
                                  form,
                                );
                                setPhoto(true);
                                setPreview(URL.createObjectURL(file));
                                setNotice(
                                  t("ছবি সংরক্ষিত হয়েছে।", "Photo saved."),
                                );
                              });
                            }}
                          />
                        </label>
                        {photo && (
                          <p role="status">
                            {t(
                              "ছবি নিরাপদে সংরক্ষিত আছে।",
                              "Photo is stored privately.",
                            )}
                          </p>
                        )}
                        {preview && (
                          <img
                            src={preview}
                            alt={t("আপনার ছবির প্রিভিউ", "Your photo preview")}
                            className="w-32 h-40 object-cover rounded-lg"
                          />
                        )}
                        <p className="notice">
                          {t(
                            "আনুষ্ঠানিক সদস্যপদের তারিখ অনুমোদনের সময় প্রশাসক নথিভুক্ত করবেন।",
                            "The administrator records your formal membership date when approving the application.",
                          )}
                        </p>
                      </div>
                    )}
                  </div>
                ) : (
                  <>
                    <dl className="review grid sm:grid-cols-2 gap-x-7">
                      {fields
                        .filter(
                          (f) =>
                            values[f.key] !== undefined &&
                            values[f.key] !== null &&
                            values[f.key] !== "" &&
                            (!f.otherOnly ||
                              !!(Number(values.helpCategories || 0) & 64)),
                        )
                        .map((f) => (
                          <div key={f.key}>
                            <dt>{label(f.key)}</dt>
                            <dd>
                              {f.options?.find(
                                (o) => String(o[0]) === String(values[f.key]),
                              )?.[lang === "bn" ? 1 : 2] ??
                                String(values[f.key])}
                            </dd>
                          </div>
                        ))}
                      <div>
                        <dt>{t("সহযোগিতার ক্ষেত্র", "Areas of help")}</dt>
                        <dd>
                          {helpOptions
                            .filter(
                              ([bit]) =>
                                !!(Number(values.helpCategories || 0) & bit),
                            )
                            .map((o) => o[lang === "bn" ? 1 : 2])
                            .join(", ")}
                        </dd>
                      </div>
                      <div>
                        <dt>{t("ছবি", "Photo")}</dt>
                        <dd>
                          {photo
                            ? t("সংরক্ষিত", "Uploaded")
                            : t("দেওয়া হয়নি", "Missing")}
                        </dd>
                      </div>
                    </dl>
                    <div className="grid gap-4">
                      {definition &&
                        (["conduct", "declaration", "oath"] as const).map(
                          (name, i) => (
                            <label className="consent" key={name}>
                              <input
                                type="checkbox"
                                checked={consents[i]}
                                onChange={(e) =>
                                  setConsents((c) =>
                                    c.map((v, j) =>
                                      i === j ? e.target.checked : v,
                                    ),
                                  )
                                }
                              />
                              <span>
                                {
                                  definition[
                                    (name +
                                      (lang === "bn"
                                        ? "Bn"
                                        : "En")) as keyof Definition
                                  ]
                                }
                              </span>
                            </label>
                          ),
                        )}
                      {definition?.version !== consentVersion && (
                        <p role="alert">
                          {t(
                            "ফর্মের শর্ত পরিবর্তিত হয়েছে। খসড়া সংরক্ষণ করে পৃষ্ঠা পুনরায় খুলুন।",
                            "Form terms have changed or could not load. Save your draft and reload before submitting.",
                          )}
                        </p>
                      )}
                    </div>
                  </>
                )}
                <div className="flex gap-3 flex-wrap mt-8">
                  <button
                    className="secondary"
                    onClick={() =>
                      void run(async () => {
                        await save();
                        setNotice(t("খসড়া সংরক্ষিত হয়েছে।", "Draft saved."));
                      })
                    }
                  >
                    {t("খসড়া সংরক্ষণ", "Save draft")}
                  </button>
                  {step > 0 && (
                    <button
                      className="secondary"
                      onClick={() => setStep((s) => s - 1)}
                    >
                      {t("পেছনে", "Back")}
                    </button>
                  )}
                  {step < 5 ? (
                    <button
                      className="primary"
                      onClick={() => {
                        if (check(step))
                          void run(async () => {
                            await save();
                            setStep((s) => s + 1);
                          });
                      }}
                    >
                      {t("সংরক্ষণ করে পরবর্তী ধাপ", "Save and continue")}
                    </button>
                  ) : (
                    <button
                      className="primary"
                      disabled={
                        !consents.every(Boolean) ||
                        definition?.version !== consentVersion
                      }
                      onClick={() => {
                        if (!check()) return;
                        void run(async () => {
                          await save();
                          const data = await request<RecordData>(
                            "applications/" +
                              credentials.referenceCode +
                              "/submit",
                            "POST",
                            {
                              ...credentials,
                              codeOfConductAccepted: consents[0],
                              declarationAccepted: consents[1],
                              oathAccepted: consents[2],
                              consentVersion,
                            },
                          );
                          absorb(data);
                        });
                      }}
                    >
                      {t("আবেদন জমা দিন", "Submit application")}
                    </button>
                  )}
                </div>
                <p className="text-sm mt-5">
                  {t(
                    "জমা দেওয়ার পর আবেদন সম্পাদনা করা যাবে না।",
                    "You cannot edit the application after submitting.",
                  )}
                </p>
              </section>
            </div>
          )}
        </fieldset>
      </div>
    </>
  );
}
