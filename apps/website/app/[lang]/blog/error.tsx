"use client";
export default function ErrorPage({ reset }: { reset: () => void }) {
  return (
    <main className="blog-shell">
      <h1>Reading is temporarily unavailable</h1>
      <p>পাঠের সেবাটি সাময়িকভাবে পাওয়া যাচ্ছে না। একটু পরে আবার চেষ্টা করুন।</p>
      <button className="primary" onClick={reset}>
        Try again / আবার চেষ্টা করুন
      </button>
    </main>
  );
}
