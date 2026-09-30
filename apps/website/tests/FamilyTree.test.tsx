import {afterEach, expect, it} from "vitest";
import {cleanup, render, screen, fireEvent} from "@testing-library/react";
import {FamilyTree} from "../components/site/FamilyTree";
afterEach(cleanup);
it("shows all known descendants immediately with only two tabs",()=>{
 render(<FamilyTree language="en"/>);
 expect(screen.getAllByRole("tab")).toHaveLength(2);
 expect(screen.getByText("Faruk Ahmed Nazim")).toBeTruthy();
 expect(screen.getByText("Sheikh Madi Ahmed")).toBeTruthy();
 expect(screen.queryByRole("searchbox")).toBeNull();
 expect(screen.queryByRole("button")).toBeNull();
 expect(screen.queryByRole("dialog")).toBeNull();
});
it("switches paternal and maternal parent information",()=>{
 render(<FamilyTree language="en"/>);
 expect(screen.getByText("Kazim Uddin Ahmad")).toBeTruthy();
 fireEvent.click(screen.getByRole("tab",{name:"Maternal family"}));
 expect(screen.getByText("Yaron Nessa")).toBeTruthy();
 expect(screen.queryByText("Kazim Uddin Ahmad")).toBeNull();
 expect(screen.getByRole("tab",{name:"Maternal family"}).getAttribute("aria-selected")).toBe("true");
 expect(screen.getByText("Faruk Ahmed Nazim")).toBeTruthy();
});
it("supports keyboard tabs and Bangla names",()=>{
 render(<FamilyTree language="bn"/>);
 fireEvent.keyDown(screen.getByRole("tab",{name:"পিতৃকুল"}),{key:"ArrowRight"});
 expect(screen.getByRole("tab",{name:"মাতৃকুল"}).getAttribute("aria-selected")).toBe("true");
 expect(screen.getByText("ইয়ারন নেসা")).toBeTruthy();
});
