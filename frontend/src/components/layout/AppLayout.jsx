import { useState } from "react";

import Header from "./Header";
import Sidebar from "./Sidebar";

import "./AppLayout.css";

function AppLayout({ children }) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="app-layout">
      <Header onToggleMenu={() => setIsOpen(true)} />

      <Sidebar
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
      />

      <main className="app-content">{children}</main>
    </div>
  );
}

export default AppLayout;