"use client";

import useTodos from "@/features/playground-todos/use-todos";
import { Checkbox } from "@/shared/ui/checkbox";
import { useState } from "react";

export default function TodoList() {
  const { todos, addTodo, toggleTodo, removeTodo, incompletedCount } =
    useTodos();
  const [text, setText] = useState("");

  return (
    <div>
      <h2>Todo List</h2>
      <h2>{incompletedCount}</h2>
      <ul>
        {todos.map((todo) => (
          <li key={todo.id}>
            {todo.text} - {todo.isCompleted ? "Completed" : "Not Completed"}
            <Checkbox onCheckedChange={() => toggleTodo(todo.id)}></Checkbox>
            <button onClick={() => removeTodo(todo.id)}>Remove</button>
          </li>
        ))}
      </ul>
      <input
        type="text"
        placeholder="введите текст"
        onChange={(event) => setText(event.target.value)}
        value={text}
      />
      <button onClick={() => addTodo(text)}>Добавить</button>
    </div>
  );
}
