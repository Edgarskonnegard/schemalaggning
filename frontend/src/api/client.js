async function request(path, options = {}) {
  const response = await fetch(path, {
    headers: {
      "Content-Type": "application/json",
      ...options.headers,
    },
    ...options,
  });

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get("content-type") || "";
  const payload = contentType.includes("application/json")
    ? await response.json()
    : await response.text();

  if (!response.ok) {
    const message =
      typeof payload === "string"
        ? payload
        : payload?.title || payload?.detail || "Något gick fel.";
    throw new Error(message);
  }

  return payload;
}

export function get(path) {
  return request(path);
}

export function post(path, data) {
  return request(path, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export function put(path, data) {
  return request(path, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export function del(path) {
  return request(path, {
    method: "DELETE",
  });
}
