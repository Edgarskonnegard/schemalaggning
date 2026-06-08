import { useId } from "react";

import "./Modal.css";

function Modal({
  actions,
  children,
  className = "",
  description,
  onClose,
  size = "default",
  title,
}) {
  const titleId = useId();

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <section
        aria-labelledby={titleId}
        aria-modal="true"
        className={`modal modal-${size} ${className}`.trim()}
        role="dialog"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="modal-header">
          <div>
            <h2 id={titleId}>{title}</h2>
            {description && <p>{description}</p>}
          </div>

          <button className="modal-close" type="button" onClick={onClose}>
            Stäng
          </button>
        </div>

        <div className="modal-body">{children}</div>

        {actions && <div className="modal-actions">{actions}</div>}
      </section>
    </div>
  );
}

export default Modal;
