"use client";
import {useId, useRef, useState} from "react";
import {familyPeople, familyRoots} from "../../../../packages/website/family-data";

export function FamilyTree({language}:{language:"bn"|"en"}) {
 const [side,setSide]=useState<keyof typeof familyRoots>("paternal");
 const tabs=useRef<(HTMLButtonElement|null)[]>([]);
 const uid=useId();
 const t=(en:string,bn:string)=>language==="bn"?bn:en;
 const sides=["paternal","maternal"] as const;
 const parent=familyPeople[familyPeople[familyRoots[side]].children![0]];
 const mawla=familyPeople.mawla;
 const card=(id:string)=>{
  const person=familyPeople[id];
  return <li key={id} className={`family-simple-card${id==="mawla"?" family-simple-focus":""}`}>
   <span>{person[language]}</span>{person.dates&&<small>{person.dates}</small>}
  </li>;
 };
 return <section className="family-section" aria-labelledby={`${uid}-heading`}>
  <h2 id={`${uid}-heading`}>{t("Family tree","বংশ পরিচয়")}</h2>
  <div className="family-tabs" role="tablist" aria-label={t("Family branch","পারিবারিক শাখা")}>
   {sides.map((value,index)=><button key={value} ref={el=>{tabs.current[index]=el;}} id={`${uid}-${value}`} role="tab" aria-selected={side===value} aria-controls={`${uid}-panel`} tabIndex={side===value?0:-1} onClick={()=>setSide(value)} onKeyDown={event=>{
    if(!["ArrowLeft","ArrowRight","Home","End"].includes(event.key))return;
    event.preventDefault();const next=event.key==="Home"?0:event.key==="End"?1:1-index;
    setSide(sides[next]);tabs.current[next]?.focus();
   }}>{value==="paternal"?t("Paternal family","পিতৃকুল"):t("Maternal family","মাতৃকুল")}</button>)}
  </div>
  <div className="family-simple-panel" id={`${uid}-panel`} role="tabpanel" aria-labelledby={`${uid}-${side}`} tabIndex={0}>
   <div className="family-parent"><small>{side==="paternal"?t("Father","পিতা"):t("Mother","মাতা")}</small><h3>{parent[language]}</h3></div>
   <section className="family-generation" aria-label={t("Mawla and siblings","মাওলা ও ভাইবোন")}>
    <h3>{t("Mawla and siblings","মাওলা ও ভাইবোন")}</h3>
    <ul className="family-simple-grid family-siblings">{(parent.children||[]).map(card)}</ul>
   </section>
   <section className="family-generation" aria-label={t("Mawla’s children","মাওলার সন্তানগণ")}>
    <h3>{t("Mawla’s children","মাওলার সন্তানগণ")}</h3>
    <ul className="family-simple-grid family-children">{(mawla.children||[]).map(card)}</ul>
   </section>
   <p className="family-review-note">{t("Provisional information; names and relationships will be reviewed. Earlier ancestors and further descendants are not yet complete.","প্রাথমিক তথ্য; নাম ও সম্পর্ক পরে যাচাই করা হবে। পূর্বপুরুষ ও পরবর্তী বংশধরদের তথ্য এখনও অসম্পূর্ণ।")}</p>
  </div>
 </section>;
}
