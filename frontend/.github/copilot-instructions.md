# Copilot Instructions for This Codebase

## Overview
This project is a React-based scheduling system ("schemaläggningssystem") built with Vite. It features a modular component structure and a simple, page-based navigation pattern. The codebase is designed for clarity and maintainability, with a focus on Swedish retail scheduling workflows.

## Architecture & Structure
- **Entry Point:** `src/main.jsx` renders the root `App` component into `#root` in `index.html`.
- **App Layout:** `src/App.jsx` manages page navigation and wraps all content in `AppLayout`.
- **Pages:**
  - `src/pages/DashboardPage.jsx`: System overview.
  - `src/pages/SchedulePage.jsx`: Monthly schedule view (uses `Calendar`).
  - `src/pages/EditSchedulePage.jsx`: Create/edit shifts (currently static).
  - `src/pages/EmployeesPage.jsx`: Manage employees (add/list).
- **Components:**
  - **Layout:** `src/components/layout/` (Header, Sidebar, AppLayout)
  - **Calendar:** `src/components/calendar/Calendar.jsx` (month grid, week numbers, employee rows, shift display)
  - **Schedule:** `src/components/schedule/` (CreateShiftForm, ScheduleList, ShiftCard)
  - **UI:** `src/components/ui/` (Button, Card, Input, Select, Badge)
- **Styling:** CSS Modules per component, global styles in `src/index.css` and `src/App.css`.

## Developer Workflows
- **Start Dev Server:** `npm run dev` (or `yarn dev`)
- **Build for Production:** `npm run build`
- **Preview Production Build:** `npm run preview`
- **Lint:** `npm run lint` (uses ESLint with React and Vite plugins)
- **No built-in test suite** (add tests as needed).

## Project Conventions
- **Component Structure:**
  - Each component has its own `.jsx` and `.css` file in a feature folder.
  - Use functional components and React hooks (`useState`).
  - Swedish is used for UI text and some variable names.
- **Navigation:**
  - Page navigation is managed via `currentPage` state in `App.jsx` and `AppLayout.jsx`.
  - Sidebar buttons update the current page.
- **Data Flow:**
  - Most data is local state (e.g., employees, shifts) within each page/component.
  - No global state management or backend integration by default.
- **Styling:**
  - Use the corresponding CSS file for each component.
  - Utility classes (e.g., `.badge`, `.button`) are in `ui/`.

## Integration Points
- **No API or backend integration** is present; all data is in-memory.
- **To add backend/API:**
  - Introduce data fetching in page components.
  - Use context or state management if data needs to be shared globally.

## Examples
- To add a new page, create a file in `src/pages/`, add it to `App.jsx` navigation, and update the Sidebar.
- To add a new UI component, place it in `src/components/ui/` with matching `.jsx` and `.css` files.

## References
- Key files: `src/App.jsx`, `src/components/layout/AppLayout.jsx`, `src/components/calendar/Calendar.jsx`, `src/pages/EmployeesPage.jsx`
- For build/lint commands, see `package.json`.
- For ESLint config, see `eslint.config.js`.

---
For questions or improvements, update this file to keep instructions current.
