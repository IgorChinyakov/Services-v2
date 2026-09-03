import { Todo } from "@/entities/playground/todo";
import { useState } from "react";

export default function useTodos() {
  const [todos, setTodos] = useState<Todo[]>([]);

  function addTodo(text: string) {
    const trimmedText = text.trim();

    if (!trimmedText) {
      return;
    }

    setTodos((current) => [
      ...current,
      {
        id: crypto.randomUUID(),
        text: trimmedText,
        isCompleted: false,
      },
    ]);
  }

  function toggleTodo(id: string) {
    setTodos((currentTodos) =>
      currentTodos.map((todo) =>
        todo.id === id ? { ...todo, isCompleted: !todo.isCompleted } : todo,
      ),
    );
  }

  function removeTodo(id: string) {
    setTodos((currentTodos) => currentTodos.filter((todo) => todo.id !== id));
  }

  const incompletedCount = todos.filter((t) => !t.isCompleted).length;

  return { todos, addTodo, toggleTodo, removeTodo, incompletedCount };
}
