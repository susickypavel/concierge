import { REST, Routes } from "discord.js";
import commands from "../src/commands"

const rest = new REST().setToken(process.env.DISCORD_BOT_TOKEN);

(async () => {
  try {
    console.log(
      `Started refreshing ${commands.length} application (/) commands.`
    );

    const data = await rest.put(
      Routes.applicationCommands(process.env.DISCORD_CLIENT_ID),
      { body: commands.map(command => command.data.toJSON()) }
    );

    console.log(data)
  } catch (error) {
    console.error(error);
  }
})();
