export type FamilyPerson = {
  id: string; bn: string; en: string; children?: string[]; spouse?: string;
  dates?: string; needsReview: boolean;
};
// Provisional transcription of the two supplied family charts. Never infer unclear names.
// Stable IDs let the same person appear in both views without duplicating their record.
export const familyPeople: Record<string, FamilyPerson> = Object.fromEntries([
  {id:"paternal-earlier",bn:"পূর্ববর্তী পিতৃপুরুষ — তথ্য যাচাই বাকি",en:"Earlier paternal ancestors — verification pending",children:["kazim"],needsReview:true},
  {id:"maternal-earlier",bn:"পূর্ববর্তী মাতৃপুরুষ — তথ্য যাচাই বাকি",en:"Earlier maternal ancestors — verification pending",children:["yaron"],needsReview:true},
  {id:"kazim",bn:"কাজিম উদ্দিন আহমদ",en:"Kazim Uddin Ahmad",spouse:"yaron",children:["helal","shamsun","abed","mawla","kamal","shahab"],needsReview:true},
  {id:"yaron",bn:"ইয়ারন নেসা",en:"Yaron Nessa",spouse:"kazim",children:["helal","shamsun","abed","mawla","kamal","shahab"],needsReview:true},
  {id:"helal",bn:"হেলাল উদ্দিন আহমদ",en:"Helal Uddin Ahmad",needsReview:true},
  {id:"shamsun",bn:"শামসুননাহার বেগম",en:"Shamsunnahar Begum",needsReview:true},
  {id:"abed",bn:"আবেদ উদ্দিন আহমদ",en:"Abed Uddin Ahmad",needsReview:true},
  {id:"mawla",bn:"সদর উদ্দিন আহ্‌মদ চিশ্‌তী",en:"Sadar Uddin Ahmad Chisty",dates:"1914–2006",children:["faruk","suraiya","mariam","nadira","dalia","ismat","sufia","anwara","madi"],needsReview:true},
  {id:"kamal",bn:"কামাল উদ্দিন আহমদ",en:"Kamal Uddin Ahmad",needsReview:true},
  {id:"shahab",bn:"শহাবুদ্দিন আহমদ",en:"Shahabuddin Ahmad",needsReview:true},
  {id:"faruk",bn:"ফারুক আহমেদ নাজিম",en:"Faruk Ahmed Nazim",needsReview:true},
  {id:"suraiya",bn:"সারওয়ার জাহান সুরাইয়া",en:"Sarwar Jahan Suraiya",needsReview:true},
  {id:"mariam",bn:"মরিয়ম আহমেদ নবী",en:"Mariam Ahmed Nabi",needsReview:true},
  {id:"nadira",bn:"নাদিরা আহমেদ মালা",en:"Nadira Ahmed Mala",needsReview:true},
  {id:"dalia",bn:"ডালিয়া পারভীন আহমেদ",en:"Dalia Parvin Ahmed",needsReview:true},
  {id:"ismat",bn:"ইসমত আরা আহমেদ শেফা",en:"Ismat Ara Ahmed Shefa",needsReview:true},
  {id:"sufia",bn:"সুফিয়া সদরউদ্দিন",en:"Sufia Sadaruddin",needsReview:true},
  {id:"anwara",bn:"আনোয়ারা আহমেদ শিবলী",en:"Anwara Ahmed Shibli",needsReview:true},
  {id:"madi",bn:"শেখ মাদি আহমেদ",en:"Sheikh Madi Ahmed",needsReview:true}
].map(person => [person.id, person]));
export const familyRoots = {paternal:"paternal-earlier",maternal:"maternal-earlier"} as const;
