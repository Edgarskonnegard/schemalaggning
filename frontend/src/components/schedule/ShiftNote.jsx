import "./ShiftNote.css";

function ShiftNote({
  title,
  time,
  children,
  className = "",
  draggable = false,
  onClick,
  onDragStart,
  onDragEnd,
  onKeyDown,
}) {
  const isKeyboardInteractive = Boolean(onKeyDown);

  return (
    <div
      className={`shift-note ${className}`.trim()}
      draggable={draggable}
      role={isKeyboardInteractive ? "button" : undefined}
      tabIndex={isKeyboardInteractive ? 0 : undefined}
      onClick={onClick}
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
      onKeyDown={onKeyDown}
    >
      <strong>{title}</strong>
      {time && <small>{time}</small>}
      {children}
    </div>
  );
}

export default ShiftNote;
